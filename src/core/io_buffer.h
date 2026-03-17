#pragma once

#include <cstdint>
#include <vector>
#include <memory>
#include <string>

namespace diskform4th {

enum class BusSpeed {
    USB2_0,
    USB3_0,
    USB3_1,
    USB4_0,
    NVMe,
    Unknown
};

class IOBuffer {
public:
    IOBuffer(BusSpeed speed, size_t system_ram_mb);
    ~IOBuffer();

    // Prevent copying
    IOBuffer(const IOBuffer&) = delete;
    IOBuffer& operator=(const IOBuffer&) = delete;

    // Get optimal buffer size based on bus speed and system RAM
    size_t get_optimal_buffer_size() const;

    // Allocate the buffer (aligned for zero-copy if possible)
    bool allocate();

    // Deallocate the buffer
    void deallocate();

    // Get pointer to the raw buffer
    uint8_t* get_data();
    size_t get_size() const;

private:
    BusSpeed bus_speed_;
    size_t system_ram_mb_;
    size_t optimal_buffer_size_;
    uint8_t* buffer_;

    size_t calculate_optimal_size();
};

class AsyncDiskWriter {
public:
    AsyncDiskWriter(const std::string& target_device);
    ~AsyncDiskWriter();

    // Prevent copying
    AsyncDiskWriter(const AsyncDiskWriter&) = delete;
    AsyncDiskWriter& operator=(const AsyncDiskWriter&) = delete;

    bool open();
    void close();

    // Submits an asynchronous read request
    bool read_async(IOBuffer& buffer, uint64_t offset, size_t length = 0);

    // Submits an asynchronous write request
    bool write_async(IOBuffer& buffer, uint64_t offset, size_t length = 0);

    // Waits for all pending asynchronous operations to complete
    bool flush_and_wait();

private:
    std::string target_device_;

#if defined(_WIN32)
    void* handle_; // HANDLE
    std::vector<void*> pending_overlapped_;
#elif defined(__linux__)
    int fd_;
    // Forward declaration of io_uring struct pointer to avoid pulling in the header everywhere
    void* ring_; // Store as void* to avoid exposing io_uring in header
    int pending_ops_ = 0;
#else
    // Fallback for other POSIX
    int fd_;
#endif
};

} // namespace diskform4th
