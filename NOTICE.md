# OpenPanel Notices

OpenPanel original source code is licensed under the MIT License.

## Third-Party Dependencies

| Dependency | Use | License |
| --- | --- | --- |
| LibreHardwareMonitorLib 0.9.6 | Read-only CPU, GPU, memory, and storage sensors | MPL-2.0 |
| HidSharp 2.6.4 | Read-only Logitech HID++ peripheral battery queries | Apache-2.0 |
| Microsoft.Web.WebView2 | Embedded dashboard UI in the WPF host | Microsoft package terms |
| NAudio.Wasapi 2.3.0 | Core Audio endpoint enumeration, volume, mute, and peak-level access | MIT |
| Lucide 1.27.0 | Tree-shaken inline SVG metric icons | ISC |
| Vite | TypeScript dashboard bundling during development/build | MIT |
| TypeScript | Dashboard type checking and compilation | Apache-2.0 |
| @types/node | Type definitions for Vite config | MIT |
| MSTest.TestFramework | .NET unit test framework | MIT |
| MSTest.TestAdapter | Visual Studio/dotnet test adapter | MIT |
| Microsoft.NET.Test.Sdk | .NET test SDK | MIT |
| PresentMon 2.5.1 | On-demand gaming frame-presentation metrics | MIT |

The Logitech HID++ battery protocol implementation and voltage curve were
adapted from the MIT-licensed
[logitray](https://github.com/ithilias/logitray) project. OpenPanel performs
read-only queries and does not install a Logitech background service.

The read-only Logi Options+ named-pipe framing and battery endpoint behavior
were informed by the MIT-licensed
[logi-cli](https://github.com/balusch/logi-cli) interoperability project.
OpenPanel connects only when the user's existing Logi Options+ agent is already
running and does not start or modify that software.

LibreHardwareMonitorLib brings these transitive packages into the restored dependency graph:

- `BlackSharp.Core` 1.0.7: shared toolkit code, MPL-2.0.
- `DiskInfoToolkit` 1.1.2: disk-access support, MPL-2.0.
- `Mono.Posix.NETStandard` 1.0.0: POSIX compatibility support included by the upstream package.
- `RAMSPDToolkit-NDD` 1.4.2: RAM SPD access, MPL-2.0.
- `System.IO.FileSystem.AccessControl` 5.0.0: filesystem access-control APIs, MIT.
- `System.IO.Ports` 10.0.3 and its platform runtime packs: serial-port APIs, MIT.
- `System.Management` 10.0.2: Windows Management Instrumentation APIs, MIT.

NAudio.Wasapi brings this MIT-licensed package into the restored dependency graph:

- `NAudio.Core`

These transitive packages are upstream requirements even though OpenPanel enables only CPU, GPU, memory, and storage hardware categories.

Do not add new dependencies without updating this file and documenting why the dependency is needed.
