# OpenPanel Notices

OpenPanel original source code is licensed under the MIT License.

## Third-Party Dependencies

| Dependency | Use | License |
| --- | --- | --- |
| Microsoft.Web.WebView2 | Embedded dashboard UI in the WPF host | Microsoft package terms |
| Vite | TypeScript dashboard bundling during development/build | MIT |
| TypeScript | Dashboard type checking and compilation | Apache-2.0 |
| @types/node | Type definitions for Vite config | MIT |
| MSTest.TestFramework | .NET unit test framework | MIT |
| MSTest.TestAdapter | Visual Studio/dotnet test adapter | MIT |
| Microsoft.NET.Test.Sdk | .NET test SDK | MIT |

Planned but not yet added:

- LibreHardwareMonitor for hardware telemetry.
- NAudio for Core Audio interop if it meaningfully reduces custom interop.
- uPlot for compact live graphs.
- Lucide icons for touch controls.

Do not add new dependencies without updating this file and documenting why the dependency is needed.
