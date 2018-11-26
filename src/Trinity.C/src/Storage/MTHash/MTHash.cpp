// Graph Engine
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//
#include "Storage/MTHash/MTHash.h"
#include "Storage/MemoryTrunk/MemoryTrunk.h"
#include "Utility/HashHelper.h"
#include <Trinity/Hash/MD5.h>
#include "MT_ENUMERATOR.h"

namespace Storage
{
    using namespace Trinity::Hash;

    uint64_t MTHash::LookupLossyCounter = 0;

    MTHash::MTHash()
    {
        BucketLockers  = nullptr;
        ExtendedInfo   = new MTHashAllocationInfo();
        FreeListLock   = new TrinityLock();
        EntryAllocLock = new TrinityLock();
    }

    MTHash::~MTHash()
    {
        DeallocateMTHash();
        if(BucketLockers) Memory::DecommitMemory(BucketLockers, BucketCount);
        delete ExtendedInfo;
        delete FreeListLock;
        delete EntryAllocLock;
    }

    void MTHash::AllocateMTHash()
    {
        char* CellEntryPtr     = memory_trunk->trunkPtr + MemoryTrunk::TrunkLength;
        char* MTEntryPtr       = CellEntryPtr + MTHash::MTEntryOffset();
        char* BucketPtr        = CellEntryPtr + MTHash::BucketMemoryOffset();
        char* BucketLockersPtr = CellEntryPtr + MTHash::BucketLockerMemoryOffset();

        uint32_t AllocatedEntryCount = ExtendedInfo->EntryCount + UInt32_Contants::GuardedEntryCount;

        CellEntries = (CellEntry*)Memory::MemoryCommit(CellEntryPtr, (size_t)(AllocatedEntryCount) << 3);
        memset((char*)CellEntries, -1, (AllocatedEntryCount << 3));

        MTEntries = (MTEntry*)Memory::MemoryCommit(MTEntryPtr, (size_t)AllocatedEntryCount << 4);

        Buckets = (int*) Memory::MemoryCommit(BucketPtr, BucketCount << 2);
        memset((char*) Buckets, -1, Memory::RoundUpToPage_32(BucketCount << 2));

        if (!TrinityConfig::ReadOnly() && BucketLockers == nullptr)
            BucketLockers = (std::atomic<char>*)Memory::MemoryCommit(BucketLockersPtr, BucketCount);

        if (MTHash::PhysicalMemoryLocking)
        {
            if (!VirtualLock(CellEntryPtr, ((size_t)AllocatedEntryCount << 3)))
                Trinity::Diagnostics::FatalError("Cannot lock the CellEntries of MTHash {0} into physical memory.", memory_trunk->TrunkId);

            if (!VirtualLock(MTEntries, ((size_t)AllocatedEntryCount << 4)))
                Trinity::Diagnostics::FatalError("Cannot lock the MTEntries of MTHash {0} into physical memory.", memory_trunk->TrunkId);

            if (!VirtualLock(BucketPtr, ((size_t) BucketCount << 2)))
                Trinity::Diagnostics::FatalError("Cannot lock the Buckets of MTHash {0} into physical memory.", memory_trunk->TrunkId);

            if (!TrinityConfig::ReadOnly() && BucketLockers != nullptr)
            {
                if (!VirtualLock(BucketLockersPtr, BucketCount))
                    Trinity::Diagnostics::FatalError("Cannot lock the BucketLockers of MTHash {0} into physical memory.", memory_trunk->TrunkId);
            }
        }
    }

    void MTHash::DeallocateMTHash()
    {
        uint32_t AllocatedEntryCount = ExtendedInfo->EntryCount + UInt32_Contants::GuardedEntryCount;
        if (PhysicalMemoryLocking)
        {
            VirtualUnlock(CellEntries, (size_t)AllocatedEntryCount << 3);
            VirtualUnlock(MTEntries, (size_t)AllocatedEntryCount << 4);
            VirtualUnlock(Buckets, BucketCount << 2);
            if (BucketLockers != nullptr)
                VirtualUnlock(BucketLockers, BucketCount);
        }

        Memory::DecommitMemory(CellEntries, (size_t)AllocatedEntryCount << 3);
        Memory::DecommitMemory(MTEntries, (size_t)AllocatedEntryCount << 4);
        Memory::DecommitMemory(Buckets, BucketCount << 2);

        ExtendedInfo->EntryCount = 0;

        CellEntries  = nullptr;
        MTEntries    = nullptr;
        Buckets      = nullptr;
    }

    void MTHash::InitMTHashAttributes(MemoryTrunk * mt)
    {
        ExtendedInfo->FreeEntryList = -1;
        ExtendedInfo->EntryCount.store(0);
        ExtendedInfo->NonEmptyEntryCount.store(0);
        ExtendedInfo->FreeEntryCount.store(0);
        this->memory_trunk            = mt;
        this->memory_trunk->hashtable = this;
    }

    void MTHash::Initialize(uint32_t capacity, MemoryTrunk * mt)
    {
        InitMTHashAttributes(mt);
        this->ExtendedInfo->EntryCount = capacity;
        AllocateMTHash();
    }

    bool MTHash::Initialize(const String& input_file, MemoryTrunk* mt)
    {
        InitMTHashAttributes(mt);
        return this->Reload(input_file);
    }

    void MTHash::Clear()
    {
        if (ExtendedInfo->NonEmptyEntryCount > 0)
        {
            for (uint32_t i = 0; i < BucketCount; i++)
            {
                Buckets[i] = -1;
            }
            memset((char*) CellEntries, -1, (size_t) (ExtendedInfo->NonEmptyEntryCount * sizeof(CellEntry)));
            ExtendedInfo->FreeEntryList      = -1;
            ExtendedInfo->NonEmptyEntryCount = 0;
            ExtendedInfo->FreeEntryCount     = 0;
        }
    }

    void MTHash::MarkTrunkDirty()
    {
        _mm_stream_si32(&LocalMemoryStorage::dirty_flags[memory_trunk->TrunkId], 1);
    }

    void MTHash::ResetSizeEntryUnsafe(int32_t index)
    {
        if (CellEntries[index].location != -1 && CellEntries[index].offset >= 0) //! NOT for large objects
            CellEntries[index].size &= 0xFFFFFF; //! Reset reserved space
    }

    uint64_t MTHash::CommittedMemorySize()
    {
        uint32_t entry_count = ExtendedInfo->EntryCount.load(std::memory_order_relaxed);
        return Memory::RoundUpToPage_32(MTHash::BucketCount << 2) /* Buckets */ + Memory::RoundUpToPage_32(MTHash::BucketCount) /* Bucket Lock */ + Memory::RoundUpToPage_32(entry_count << 3) /* CellEntries */ + Memory::RoundUpToPage_32(entry_count << 4) /* MTEntries */;
    }

    uint64_t MTHash::TotalCellSize()
    {
        uint64_t size = 0;
        for (int32_t i = 0; i < ExtendedInfo->NonEmptyEntryCount; i++)
        {
            if (CellEntries[i].location != -1)
                size += CellSize(i);
        }
        return size;
    }

    TrinityErrorCode MTHash::GetMD5Hash(OUT char* hash_buffer)
    {
        Hash::MD5 md5;
        if (!md5.good())
            return TrinityErrorCode::E_FAILURE;

        MT_ENUMERATOR it(this);
        bool null_cellptr = false;
        while (it.MoveNext() == TrinityErrorCode::E_SUCCESS)
        {
            if (nullptr == it.CellPtr()) 
            { 
                null_cellptr = true; 
                continue;
            }
            md5.hash(it.CellPtr(), (uint32_t) it.CellSize());
        }

        if (null_cellptr)
        {
            /**
             * !Note, when null cell pointers are detected, it is very likely that it is caused by a missing .lo file,
             * because memory pool cells all have addresses of the form TrunkPtr + offset, not likely null, but if
             * the .lo file is missing, during LoadLO() we simply initialize all LO slots to null. This log will provide
             * extra information in the situation, before LoadStorage() finally decides that the checksum mismatches.
             */
            Diagnostics::WriteLine(Diagnostics::LogLevel::Error, "MTHash {0}: null cell pointers detected. Missing large object file?", memory_trunk->TrunkId);
        }

        md5.getValue(hash_buffer);

        return TrinityErrorCode::E_SUCCESS;
    }
}