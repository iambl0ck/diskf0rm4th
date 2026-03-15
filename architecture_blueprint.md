# diskf0rm4th - Architecture Blueprint

## Overview
diskf0rm4th is a high-performance disk formatting and ISO imaging tool. It serves as a modern alternative to tools like Rufus, combining unparalleled speed with a clean, extensible architecture.

## Core Language & Architecture
- **Shared C++ Core Library (`src/core`)**: The heart of diskf0rm4th. Built for maximum performance and true cross-platform capability.
- **C# Bindings (P/Invoke)**: For Windows GUI (WinUI 3/WPF) and Windows PowerShell integration, maintaining native feel and performance while leveraging a modern UI stack.
- **Linux Terminal (`src/cli`)**: A lightweight, professional CLI utilizing the shared C++ core, avoiding wrapper overhead.

## High-Speed I/O Optimization
The core engine prioritizes zero-copy and asynchronous writes to bypass the OS file-system caching overhead.

1. **OS-Native Asynchronous I/O**:
   - **Linux**: Target minimum kernel 5.10+ to fully exploit `io_uring` for true asynchronous I/O and near-zero context switching.
   - **Windows**: Utilize `Overlapped I/O` to achieve simultaneous multi-threaded block writing directly to the physical drive handle.

2. **Dynamic Buffer Sizing**:
   - The buffer scaling algorithm dynamically adjusts the read/write block sizes based on available system RAM and the target device's bus speed (USB 2.0 vs 3.0/3.1/4.0/NVMe).

## Disk Formatting Logic
diskf0rm4th employs a hybrid approach to partition creation and formatting:
1. **Standard Formats (NTFS, FAT32)**: Direct wrappers around native OS formatting APIs to guarantee maximum compatibility and stability.
2. **High-Speed Quick Format**: Custom header creation and block zeroing logic directly written to the drive for the absolute fastest "Quick Format" possible when safe.

## "Pro" Features
Five key differentiators set diskf0rm4th apart:
1. **Cloud ISO Fetching**: Direct, high-speed ISO downloading straight to RAM or target drive.
2. **Real-time S.M.A.R.T. Health Monitoring**: Continuous monitoring of disk health and temperature during formatting/burning to prevent catastrophic hardware failure.
3. **Multi-drive Simultaneous Burning**: Capable of writing a single ISO to 2 or more USB drives concurrently, fully saturating the USB host controller bus.
4. **Automated Post-install Script Injection**: Seamlessly inject `unattend.xml` (Windows) or bash scripts (Linux) into the flashed drive for fully automated installations.
5. **Bit-by-bit Integrity Hashing**: Real-time or post-burn SHA-256 verification of the written data.
