# Graph Engine - Open Source

| - | Windows Multi Targeting | Ubuntu 16.04 .NET Core |
|:------:|:------:|:------:|
|Build|[<img src="https://trinitygraphengine.visualstudio.com/_apis/public/build/definitions/4cfbb293-cd2c-4f49-aa03-06894081c93b/3/badge"/>](https://trinitygraphengine.visualstudio.com/trinity-ci/_build/index?definitionId=3)|[<img src="https://trinitygraphengine.visualstudio.com/_apis/public/build/definitions/4cfbb293-cd2c-4f49-aa03-06894081c93b/4/badge"/>](https://trinitygraphengine.visualstudio.com/trinity-ci/_build/index?definitionId=4)|
|Tests|_|_|
|Stress|_|_|

This repository contains the source code of [Graph Engine][graph-engine] and its graph
query language -- [Language Integrated Knowledge Query][likq] (LIKQ).



Microsoft Graph Engine is a distributed
in-memory data processing engine, underpinned by a strongly-typed
in-memory key-value store and a general distributed computation
engine.


[LIKQ][likq-gh]
is a versatile graph query language on top of Graph Engine that
combines the capability of fast graph exploration with the flexibility
of lambda expression. Server-side computations can be expressed in
lambda expressions, embedded in LIKQ, and executed server side
during graph traversal.  LIKQ is powering [Academic Graph Search
API][academic-graph-search], part of Microsoft Cognitive Services.

## How to Contribute

If you are interested in contributing to the code, please fork the
repository and submit pull requests to the `master` branch.

Pull requests, issue reports, and suggestions are welcome.

Please submit bugs and feature requests as [GitHub Issues](https://github.com/Microsoft/GraphEngine/issues).


## Getting started with Graph Engine

### Downloads

**Recommended:** Install by searching for "Graph Engine" in the Visual Studio UI under `Tools` > `Extensions and Updates`

It can also be downloaded from [Visual
Studio Gallery][gallery].

NuGet packages [Graph Engine Core][graph-engine-core] and [LIKQ][likq-nuget] are available in the NuGet Gallery.

Graph Engine is regularly released with bug fixes and feature enhancements.

### Building for Windows

Install [Visual Studio 2017][vs], making sure to include the following workloads and components:

- .NET desktop development
- Desktop development with C++
- cmake
- `.NET Core SDK 2.0` or above

The Windows build will generate multi-targeting nuget packages for all the available modules.
Run `tools/build.ps1` with `powershell` to setup a workspace folder `build`, and build using `cmake`.

The Linux native assemblies will be automatically packaged (pre-built at `lib`), allowing the
Windows build to also work for Linux `.Net Core`.

Nuget packages will be built and put at
`build/GraphEngine**._version_.nupkg`. The folder `build/` will be
registered as a local NuGet repository and the local package cache for
`GraphEngine.**` will be cleared. After the packages are built, you
can run `dotnet restore` to use the newly built package.

### Building for Linux

Install `libunwind8`, `g++`, `cmake` and `libssl-dev`.
Install the `dotnet` package following [the official guide][dotnet-guide].

Execute `tools/build.sh`.

The Windows native assemblies will be automatically packaged, so the
Linux build will also work for Windows .Net Core.

**Because targeting `.Net Framework` is not supported**, the packages built on Linux are not
equivalent to their Windows builds, and will only support `.Net Core`.

Nuget packages will be built and put at
`build/GraphEngine**._version_.nupkg`. The folder `build/` will be
registered as a local NuGet repository and the local package cache for
`GraphEngine.Core` will be cleared. After the packages are built, you
can run `dotnet restore` to use the newly built package.


**Note:** the build script currently only supports `Ubuntu 16.04`.

## License

Copyright (c) Microsoft Corporation. All rights reserved.

Licensed under the [MIT][license] License.


<!--
Links
-->

[graph-engine]: http://www.graphengine.io/

[likq]: https://www.graphengine.io/video/likq.video.html

[likq-gh]: https://github.com/Microsoft/GraphEngine/tree/master/src/Modules/LIKQ

[academic-graph-search]: https://azure.microsoft.com/en-us/services/cognitive-services/academic-knowledge/

[gallery]: https://visualstudiogallery.msdn.microsoft.com/12835dd2-2d0e-4b8e-9e7e-9f505bb909b8

[graph-engine-core]: https://www.nuget.org/packages/GraphEngine.Core/

[likq-nuget]: https://www.nuget.org/packages/GraphEngine.LIKQ/

[vs]: https://www.visualstudio.com/

[dotnet-guide]: https://www.microsoft.com/net/learn/get-started/linuxubuntu
[license]: LICENSE.md
