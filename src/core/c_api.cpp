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

EXPORT int WriteIsoAsync(const char* target, const char* isoPath, bool smartMonitor, ProgressCallback callback) {
    // Basic mock implementation of C-API export that simulates an async write process
    diskform4th::AsyncDiskWriter writer(target);
    // if (!writer.open()) return -1; // Disabled for mock since "E:" doesn't exist on Linux host

    // Simulate ISO writing with progress callbacks
    double total_size_mb = 2500.0; // 2.5 GB ISO
    double current_written = 0;
    double speed_mb_ps = 185.5; // High speed I/O simulation

    while (current_written < total_size_mb) {
        std::this_thread::sleep_for(std::chrono::milliseconds(250)); // Update 4 times a second

        current_written += (speed_mb_ps * 0.25);
        if (current_written > total_size_mb) current_written = total_size_mb;

        int percentage = static_cast<int>((current_written / total_size_mb) * 100);

        // Add some noise to the speed for realism
        double current_speed = speed_mb_ps + ((rand() % 20) - 10);
        int remaining_seconds = static_cast<int>((total_size_mb - current_written) / current_speed);

        if (callback) {
            callback(percentage, current_speed, remaining_seconds);
        }
    }

    writer.close();
    return 0;
}

EXPORT int FormatDisk(const char* target, bool quick, ProgressCallback callback) {
    if (callback) {
        callback(0, 0, 5);
        std::this_thread::sleep_for(std::chrono::seconds(1));
        callback(50, 0, 2);
        std::this_thread::sleep_for(std::chrono::seconds(1));
        callback(100, 0, 0);
    }
    return 0; // Success mock
}

} // extern "C"