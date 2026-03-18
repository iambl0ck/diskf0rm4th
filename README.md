<div align="center">

# 💿 diskf0rm4th

**The Ultimate Forensic-Grade, God-Tier USB/Disk Imaging Tool**

[![Build Status](https://github.com/iambl0ck/diskf0rm4th/actions/workflows/build-release.yml/badge.svg)](https://github.com/iambl0ck/diskf0rm4th/actions/workflows/build-release.yml)
[![GitHub Release](https://img.shields.io/github/v/release/iambl0ck/diskf0rm4th?style=flat-square)](https://github.com/iambl0ck/diskf0rm4th/releases)
[![License](https://img.shields.io/github/license/iambl0ck/diskf0rm4th?style=flat-square)](LICENSE)

*Inspired by Rufus. Ascended by Architecture.*

[**Download Latest Release**](#) &nbsp;&bull;&nbsp; [**Documentation**](#) &nbsp;&bull;&nbsp; [**Report Bug**](#)

</div>

---

## ⚡ The Singularity is Online

**diskf0rm4th** isn't just an ISO burner. It's a high-performance, cryptographic powerhouse designed to push modern hardware to its absolute mathematical limits. Featuring multi-threaded asynchronous I/O, native hardware TRNGs, and true plausible deniability encryption, this is the final USB formatting tool you will ever need.

Whether you're deploying a vanilla Windows image, flashing a multi-boot Linux Swiss-Army knife, or scrubbing a drive to DoD 5220.22-M specifications—**diskf0rm4th** operates seamlessly at maximum theoretical bus speeds with zero context-switching overhead.

---

## 🚀 Extreme Features

| Feature | Description | Architecture |
|---------|-------------|--------------|
| 🧬 **Zero-Resource I/O** | `MapViewOfFile` / `mmap` zero-copy memory mapping for completely non-blocking, kernel-level file reads bypassing CPU context overhead. | *Native C++* |
| 🧮 **AVX-512 SHA-256** | Intel SHA extensions and AVX-512 SIMD instructions process block verification hashing at memory-bandwidth speeds (`_mm_sha256msg1_epu32`). | *Intrinsics* |
| 🗜️ **Zstd DirectStorage** | Multi-threaded Zstd compression (`ZSTD_compressStream2`) combined with NVMe-bypassing DirectStorage integration. | *Zstd API* |
| 💀 **Hardware TRNG Wipe** | Pass 3 of the DoD 5220.22-M secure wipe uses intrinsic hardware True Random Number Generators (`_rdseed64_step` / `_rdrand64_step`). | *CPU Silcon* |
| 👁️‍🗨️ **Plausible Deniability** | Real `EVP_aes_256_xts` OpenSSL cryptographic block formatting creates nested Hidden Volumes within unused partition space. | *OpenSSL EVP* |
| 🔐 **Custom MOK UEFI** | On-the-fly X.509 certificate generation and native EFI binary signing (`grubx64.efi`) completely in-memory using `libcrypto`. | *OpenSSL API* |
| 🔒 **Firmware Security** | Post-burn raw SCSI Pass-Through commands (`IOCTL_SCSI_PASS_THROUGH_DIRECT` / `SG_IO`) instantly hardware-lock the USB drive read-only. | *Kernel Mode* |
| 📱 **Air-Gapped QR Validation** | End-to-end Air-Gapped SHA-256 QR validation rendering to cross-verify the cryptographic integrity of a flashed image dynamically. | *Lock-Free IPC* |
| 🚄 **Lock-Free SPSC IPC** | A native `std::atomic` Lock-Free Ring Buffer streams C++ engine I/O stats to the C# WPF UI without thread-blocking mutexes. | *C++20* |

---

## 💻 Platforms & Binaries

- **Windows GUI**: A high-end WPF / WinUI 3 graphical tool deployed as a Standalone Single-File Executable.
- **Linux Terminal**: A professional C++ CLI utilizing native kernel calls (`open`, `mmap`, `ioctl`).
- **PowerShell / Scripting**: Exposes a raw C-API (`diskform4th_core.dll`) for enterprise SysAdmin deployments.

---

## 📖 CLI Usage

```bash
./diskform4th_cli [options]
```

### Options

* `-i, --iso <path>` : The path to the ISO file to burn.
* `-d, --device <dev>` : The physical block device target (e.g. `\\.\PhysicalDrive1` or `/dev/sdb`).
* `-w, --wipe` : Trigger DoD 5220.22-M Secure Erase before formatting.
* `-e, --encrypt` : Trigger Plausible Deniability Hidden Volume generation in remaining space.
* `-m, --multiboot` : Generate EFI Multi-Boot structure with Custom MOK signed GRUB2 embedded.
* `-v, --verify` : Read-back blocks and compute OpenCL/AVX-512 SHA-256 hardware validation.

### Examples

**Standard Image Burn:**
```bash
./diskform4th_cli -i ubuntu-24.04-desktop-amd64.iso -d /dev/sdb
```

**Forensic Secure Wipe & Plausible Deniability Formatting:**
```bash
./diskform4th_cli -i tails-amd64-5.21.iso -d /dev/sdc -w -e
```

---

<div align="center">
<i>Forged in the abyss. Built for the Singularity.</i><br>
<b><a href="https://github.com/iambl0ck/diskf0rm4th">iambl0ck/diskf0rm4th</a></b>
</div>
