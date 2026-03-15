#include "io_buffer.h"

#include <iostream>
#include <stdexcept>
#include <cstdlib>

#if defined(_WIN32)
#include <windows.h>
#elif defined(__linux__)
#include <fcntl.h>
#include <unistd.h>
#include <sys/ioctl.h>
#include <sys/stat.h>
#include <liburing.h>
#include <cstring>
#else
#include <fcntl.h>
#include <unistd.h>
#endif

namespace diskform4th {

IOBuffer::IOBuffer(BusSpeed speed, size_t system_ram_mb)
    : bus_speed_(speed), system_ram_mb_(system_ram_mb), optimal_buffer_size_(0), buffer_(nullptr) {
    optimal_buffer_size_ = calculate_optimal_size();
}

IOBuffer::~IOBuffer() {
    deallocate();
}

size_t IOBuffer::calculate_optimal_size() {
    // Basic heuristic: Maximize buffer size up to a limit based on interface speed
    // while keeping memory footprint well under total system RAM.

    // Default fallback size (e.g. 1MB)
    size_t default_size = 1024 * 1024;

    // Determine max reasonable buffer for interface
    size_t target_size;
    switch (bus_speed_) {
        case BusSpeed::USB2_0:  target_size = 2 * 1024 * 1024; break;    // 2MB (USB 2 is slow, keep buffers small)
        case BusSpeed::USB3_0:  target_size = 16 * 1024 * 1024; break;   // 16MB
        case BusSpeed::USB3_1:  target_size = 64 * 1024 * 1024; break;   // 64MB
        case BusSpeed::USB4_0:  target_size = 128 * 1024 * 1024; break;  // 128MB
        case BusSpeed::NVMe:    target_size = 256 * 1024 * 1024; break;  // 256MB
        case BusSpeed::Unknown:
        default:                target_size = default_size; break;
    }

    // Cap at a safe percentage of system RAM (e.g., max 5%)
    size_t max_ram_mb = (system_ram_mb_ * 5) / 100;
    if (target_size > (max_ram_mb * 1024 * 1024)) {
        target_size = (max_ram_mb * 1024 * 1024);
    }

    // Ensure we don't go below 1MB
    if (target_size < default_size) {
         target_size = default_size;
    }

    // Align to 4KB (standard page size) for zero-copy DMA compatibility
    target_size = (target_size + 4095) & ~4095;

    return target_size;
}

size_t IOBuffer::get_optimal_buffer_size() const {
    return optimal_buffer_size_;
}

bool IOBuffer::allocate() {
    if (buffer_) return true; // Already allocated

#if defined(_WIN32)
    // VirtualAlloc gives us page-aligned memory suitable for unbuffered I/O
    buffer_ = static_cast<uint8_t*>(VirtualAlloc(NULL, optimal_buffer_size_, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE));
#elif defined(_POSIX_VERSION) || defined(__linux__)
    // posix_memalign aligns the buffer for O_DIRECT / zero-copy
    void* mem = nullptr;
    if (posix_memalign(&mem, 4096, optimal_buffer_size_) == 0) {
        buffer_ = static_cast<uint8_t*>(mem);
    }
#else
    buffer_ = new (std::nothrow) uint8_t[optimal_buffer_size_];
#endif

    return buffer_ != nullptr;
}

void IOBuffer::deallocate() {
    if (buffer_) {
#if defined(_WIN32)
        VirtualFree(buffer_, 0, MEM_RELEASE);
#elif defined(_POSIX_VERSION) || defined(__linux__)
        free(buffer_);
#else
        delete[] buffer_;
#endif
        buffer_ = nullptr;
    }
}

uint8_t* IOBuffer::get_data() {
    return buffer_;
}

size_t IOBuffer::get_size() const {
    return optimal_buffer_size_;
}


// --- AsyncDiskWriter ---

AsyncDiskWriter::AsyncDiskWriter(const std::string& target_device)
    : target_device_(target_device) {
#if defined(_WIN32)
    handle_ = INVALID_HANDLE_VALUE;
#elif defined(__linux__)
    fd_ = -1;
    struct io_uring* ur = new struct io_uring;
    io_uring_queue_init(32, ur, 0); // Initialize a 32-entry submission queue
    ring_ = ur;
#else
    fd_ = -1;
#endif
}

AsyncDiskWriter::~AsyncDiskWriter() {
    close();
#if defined(__linux__)
    if (ring_) {
        struct io_uring* ur = static_cast<struct io_uring*>(ring_);
        io_uring_queue_exit(ur);
        delete ur;
        ring_ = nullptr;
    }
#endif
}

bool AsyncDiskWriter::open() {
#if defined(_WIN32)
    // Open for raw disk access with FILE_FLAG_NO_BUFFERING and FILE_FLAG_OVERLAPPED
    handle_ = CreateFileA(
        target_device_.c_str(),
        GENERIC_WRITE,
        FILE_SHARE_READ | FILE_SHARE_WRITE,
        NULL,
        OPEN_EXISTING,
        FILE_FLAG_NO_BUFFERING | FILE_FLAG_WRITE_THROUGH | FILE_FLAG_OVERLAPPED,
        NULL
    );
    if (handle_ == INVALID_HANDLE_VALUE) {
        std::cerr << "Failed to open device: " << target_device_ << std::endl;
        return false;
    }
#elif defined(__linux__)
    // Open with O_DIRECT to bypass page cache (zero-copy) and O_SYNC for durability
    fd_ = ::open(target_device_.c_str(), O_WRONLY | O_DIRECT | O_SYNC);
    if (fd_ < 0) {
        std::cerr << "Failed to open device: " << target_device_ << std::endl;
        return false;
    }
#else
    fd_ = ::open(target_device_.c_str(), O_WRONLY | O_SYNC);
    if (fd_ < 0) {
        return false;
    }
#endif
    return true;
}

void AsyncDiskWriter::close() {
#if defined(_WIN32)
    if (handle_ != INVALID_HANDLE_VALUE) {
        CloseHandle(handle_);
        handle_ = INVALID_HANDLE_VALUE;
    }
#else
    if (fd_ >= 0) {
        ::close(fd_);
        fd_ = -1;
    }
#endif
}

bool AsyncDiskWriter::write_async(IOBuffer& buffer, uint64_t offset) {
    if (!buffer.get_data()) {
        std::cerr << "Buffer not allocated!" << std::endl;
        return false;
    }

#if defined(_WIN32)
    OVERLAPPED* overlapped = new OVERLAPPED;
    ZeroMemory(overlapped, sizeof(OVERLAPPED));
    overlapped->Offset = offset & 0xFFFFFFFF;
    overlapped->OffsetHigh = offset >> 32;
    overlapped->hEvent = CreateEvent(NULL, TRUE, FALSE, NULL);

    DWORD bytes_written = 0;
    // WriteFile with Overlapped I/O
    BOOL result = WriteFile(
        handle_,
        buffer.get_data(),
        static_cast<DWORD>(buffer.get_size()),
        &bytes_written,
        overlapped
    );

    if (!result && GetLastError() != ERROR_IO_PENDING) {
        std::cerr << "WriteFile failed!" << std::endl;
        CloseHandle(overlapped->hEvent);
        delete overlapped;
        return false;
    }
    pending_overlapped_.push_back(overlapped);
    return true;
#elif defined(__linux__)
    if (fd_ < 0 || !ring_) return false;

    struct io_uring* ur = static_cast<struct io_uring*>(ring_);

    // Get a submission queue entry (SQE)
    struct io_uring_sqe *sqe = io_uring_get_sqe(ur);
    if (!sqe) {
        std::cerr << "Could not get io_uring SQE" << std::endl;
        return false;
    }

    // Prepare a write operation
    io_uring_prep_write(sqe, fd_, buffer.get_data(), buffer.get_size(), offset);

    // Tell kernel we have an SQE ready
    int ret = io_uring_submit(ur);
    if (ret < 0) {
         std::cerr << "io_uring_submit failed: " << strerror(-ret) << std::endl;
         return false;
    }
    pending_ops_++;
    return true;
#else
    // Fallback blocking write
    if (fd_ < 0) return false;
    if (lseek(fd_, offset, SEEK_SET) == (off_t)-1) return false;
    ssize_t written = ::write(fd_, buffer.get_data(), buffer.get_size());
    return written == buffer.get_size();
#endif
}

bool AsyncDiskWriter::flush_and_wait() {
#if defined(_WIN32)
    bool success = true;
    for (void* ptr : pending_overlapped_) {
        OVERLAPPED* overlapped = static_cast<OVERLAPPED*>(ptr);
        DWORD bytes_transferred;
        if (!GetOverlappedResult(handle_, overlapped, &bytes_transferred, TRUE)) {
            success = false;
        }
        CloseHandle(overlapped->hEvent);
        delete overlapped;
    }
    pending_overlapped_.clear();

    if (handle_ != INVALID_HANDLE_VALUE) {
        if (!FlushFileBuffers(handle_)) {
            success = false;
        }
    }
    return success;
#elif defined(__linux__)
    if (!ring_) return false;
    struct io_uring* ur = static_cast<struct io_uring*>(ring_);

    bool success = true;
    while (pending_ops_ > 0) {
        struct io_uring_cqe *cqe;
        int ret = io_uring_wait_cqe(ur, &cqe);
        if (ret < 0) {
             std::cerr << "io_uring_wait_cqe failed: " << strerror(-ret) << std::endl;
             success = false;
             break; // Fatal error
        }

        if (cqe->res < 0) {
            std::cerr << "Async write failed: " << strerror(-cqe->res) << std::endl;
            success = false;
        }

        io_uring_cqe_seen(ur, cqe);
        pending_ops_--;
    }
    return success;
#else
    if (fd_ >= 0) {
#if defined(__APPLE__)
        return fcntl(fd_, F_FULLFSYNC) != -1;
#else
        return fdatasync(fd_) == 0;
#endif
    }
    return false;
#endif
}

} // namespace diskform4th
