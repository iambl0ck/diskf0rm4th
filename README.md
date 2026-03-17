# diskf0rm4th

**A high-performance disk formatting and ISO imaging tool inspired by Rufus, built with modern UI and advanced speed.**

diskf0rm4th utilizes a shared C++ core to deliver unmatched write speeds by heavily optimizing block-level I/O (`io_uring` on Linux and `Overlapped I/O` on Windows).

## Features

- **Blazing Fast I/O Engine**: Multi-threaded asynchronous I/O with dynamic buffer sizing for zero-copy block writing.
- **Universal Multi-Platform Core**: High-speed C++ engine powering CLI, PowerShell, and native GUI (WinUI 3/WPF) versions.
- **Advanced Formatting Logic**: Hybrid OS-wrapper and custom low-level quick-formatting algorithms for maximum stability and speed.

## Pro Features

- **Cloud ISO Fetching**: Download and burn ISOs directly to RAM/Drive.
- **Real-time S.M.A.R.T. Monitoring**: Monitor disk health during intense format and write operations.
- **Multi-drive Simultaneous Burning**: Write an ISO to multiple drives simultaneously.
- **Automated Post-install Script Injection**: Inject `unattend.xml` or custom scripts.
- **Bit-by-bit Integrity Hashing**: Real-time or post-burn SHA-256 validation.

## Architecture

See `architecture_blueprint.md` for a deep dive into the underlying architecture and I/O buffer module design.

## Building the Core Engine

### Prerequisites
- Linux: Kernel 5.10+, GCC/Clang, `liburing-dev`
- Windows: Visual Studio (C++ Desktop Development), MSBuild

### Compilation (CMake)
```bash
mkdir build
cd build
cmake ..
make
```

### License
This project is licensed under the MIT License - see the `LICENSE` file for details.
