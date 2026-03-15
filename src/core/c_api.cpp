#include "io_buffer.h"

#if defined(_WIN32)
#define EXPORT __declspec(dllexport)
#else
#define EXPORT __attribute__((visibility("default")))
#endif

extern "C" {

EXPORT int WriteIsoAsync(const char* target, const char* isoPath, bool smartMonitor) {
    // Basic mock implementation of C-API export
    diskform4th::AsyncDiskWriter writer(target);
    if (!writer.open()) return -1;

    // Logic for writing ISO would go here...

    writer.close();
    return 0;
}

EXPORT int FormatDisk(const char* target, bool quick) {
    // Basic mock implementation of C-API export
    return 0; // Success mock
}

} // extern "C"