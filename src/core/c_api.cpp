#include "io_buffer.h"
#include <thread>
#include <chrono>

#if defined(_WIN32)
#define EXPORT __declspec(dllexport)
#else
#define EXPORT __attribute__((visibility("default")))
#endif

extern "C" {

typedef void (*ProgressCallback)(int percentage, double speedMbPs, int remainingSeconds);

EXPORT int WriteIsoAsync(const char* target, const char* isoPath, bool smartMonitor, ProgressCallback callback);
EXPORT int FormatDisk(const char* target, bool quick, ProgressCallback callback);

} // extern "C"

#include <fstream>
#include <iostream>

EXPORT int WriteIsoAsync(const char* target, const char* isoPath, bool smartMonitor, ProgressCallback callback) {
    diskform4th::AsyncDiskWriter writer(target);

    // Safety check: Don't open if target is empty
    if (!target || target[0] == '\0') return -1;

#if defined(__linux__)
    // Prevent accidental formatting of root on Linux (Safety Lock)
    std::string tgt(target);
    if (tgt == "/" || tgt == "/dev/sda" || tgt == "/dev/nvme0n1" || tgt == "/dev/vda") {
        std::cerr << "CRITICAL SAFETY LOCK: Attempt to write to system drive " << tgt << " blocked." << std::endl;
        return -2;
    }
#endif

    std::ifstream iso_file(isoPath, std::ios_base::binary | std::ios_base::ate);
    if (!iso_file.is_open()) {
        std::cerr << "Failed to open ISO file: " << isoPath << std::endl;
        return -3;
    }

    uint64_t total_size = iso_file.tellg();
    iso_file.seekg(0, std::ios::beg);

    if (!writer.open()) {
        std::cerr << "Failed to open target device: " << target << std::endl;
        return -4;
    }

    // Allocate an IOBuffer dynamically based on system RAM and bus speed (mocking system ram detection for now)
    diskform4th::IOBuffer io_buf(diskform4th::BusSpeed::USB3_0, 16384); // 16GB RAM assumed
    if (!io_buf.allocate()) {
        std::cerr << "Failed to allocate IOBuffer" << std::endl;
        writer.close();
        return -5;
    }

    size_t chunk_size = io_buf.get_size();
    uint8_t* buffer_ptr = io_buf.get_data();
    uint64_t bytes_written = 0;

    auto start_time = std::chrono::steady_clock::now();
    auto last_callback_time = start_time;

    while (bytes_written < total_size) {
        size_t bytes_to_read = std::min(static_cast<uint64_t>(chunk_size), total_size - bytes_written);

        // Read from ISO to buffer
        if (!iso_file.read(reinterpret_cast<char*>(buffer_ptr), bytes_to_read)) {
            std::cerr << "Error reading from ISO file at offset " << bytes_written << std::endl;
            writer.close();
            return -6;
        }

        // Ensure block alignment for the last write, as O_DIRECT / FILE_FLAG_NO_BUFFERING requires sector alignment
        size_t aligned_bytes_to_read = (bytes_to_read + 4095) & ~4095;

        // Submit async write to target device
        if (!writer.write_async(io_buf, bytes_written, aligned_bytes_to_read)) {
             std::cerr << "Async write failed at offset " << bytes_written << std::endl;
             writer.close();
             return -7;
        }

        // Wait for the async write block to complete before continuing
        // In a true zero-copy multi-buffer architecture, we'd use a ring buffer of IOBuffers here.
        if (!writer.flush_and_wait()) {
             std::cerr << "Async flush/wait failed at offset " << bytes_written << std::endl;
             writer.close();
             return -8;
        }

        bytes_written += bytes_to_read;

        // Progress Calculation (throttle UI updates to ~4 times a second)
        auto current_time = std::chrono::steady_clock::now();
        std::chrono::duration<double> elapsed_since_last_cb = current_time - last_callback_time;

        if (elapsed_since_last_cb.count() >= 0.25 || bytes_written == total_size) {
            std::chrono::duration<double> total_elapsed = current_time - start_time;

            int percentage = static_cast<int>((static_cast<double>(bytes_written) / total_size) * 100);

            double speed_mb_ps = 0.0;
            if (total_elapsed.count() > 0) {
                speed_mb_ps = (bytes_written / (1024.0 * 1024.0)) / total_elapsed.count();
            }

            int remaining_seconds = 0;
            if (speed_mb_ps > 0) {
                 double remaining_mb = (total_size - bytes_written) / (1024.0 * 1024.0);
                 remaining_seconds = static_cast<int>(remaining_mb / speed_mb_ps);
            }

            if (callback) {
                callback(percentage, speed_mb_ps, remaining_seconds);
            }
            last_callback_time = current_time;
        }
    }

    // Final hardware cache flush
#if defined(_WIN32)
    // Would call FlushFileBuffers here, but typically handled by OS when closing handle for removable drives
#elif defined(__linux__)
    // Explicit sync for linux
    system("sync");
#endif

    writer.close();
    return 0;
}

EXPORT int FormatDisk(const char* target, bool quick, ProgressCallback callback) {
    // Safety check
    if (!target || target[0] == '\0') return -1;

#if defined(__linux__)
    std::string tgt(target);
    if (tgt == "/" || tgt.find("/dev/sda") == 0 || tgt.find("/dev/nvme0n1") == 0 || tgt.find("/dev/vda") == 0) {
        std::cerr << "CRITICAL SAFETY LOCK: Attempt to format system drive " << tgt << " blocked." << std::endl;
        return -2;
    }
#endif

    // Wrapping OS format commands as per architectural blueprint
    if (callback) callback(10, 0.0, 10);

#if defined(_WIN32)
    // Using simple format command for mockup, actual impl would use Virtual Disk Service (VDS) or WMI
    std::string cmd = "format " + std::string(target) + " /FS:NTFS /Q /Y";
    int ret = system(cmd.c_str());
#else
    std::string cmd = "mkfs.ntfs -f " + std::string(target) + " >/dev/null 2>&1"; // -f is fast/quick
    int ret = system(cmd.c_str());
#endif

    if (callback) callback(100, 0.0, 0);
    return ret == 0 ? 0 : -3;
}