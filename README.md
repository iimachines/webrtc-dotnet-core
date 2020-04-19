
# NOTE: This project is no longer maintained

* Prequisites
    * a modern NVidia card installed with NVENC support
    * Visual Studio 2017
        * You should have installed the ASP.NET, C++ Desktop, and .NET Core workloads
    * GIT LFS
        * If you installed it after cloning, run `git submodule foreach --recursive git lfs pull`
* Open Visual Studio 2017
* Load the `webrtc-dotnet-core.sln` solution
* Set the `webrtc-dotnet-web-demo` as startup project
* Build and run the `x64` target
* Open a webpage at `https://localhost:3000` 
    * Only tested on Windows Chrome and MacOS Safari
* Clip on the right page
* You should see a bouncing ball in the web browser
