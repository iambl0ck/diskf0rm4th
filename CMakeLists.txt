cmake_minimum_required(VERSION 3.10)
project(diskform4th CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED True)

# Platform specific configurations
find_package(OpenCL REQUIRED)
find_package(PkgConfig REQUIRED)
find_package(OpenSSL REQUIRED)
pkg_check_modules(ZSTD REQUIRED libzstd)

if (CMAKE_SYSTEM_NAME MATCHES "Linux")
    find_package(PkgConfig REQUIRED)
    pkg_check_modules(LIBURING REQUIRED liburing)
endif()

# Enable RDRAND intrinsics for compiler
set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -mrdrnd -mrdseed")

# Core library
add_library(diskform4th_core SHARED src/core/io_buffer.cpp src/core/c_api.cpp)
target_include_directories(diskform4th_core PUBLIC src/core)
target_include_directories(diskform4th_core PUBLIC ${OpenCL_INCLUDE_DIRS})
target_link_libraries(diskform4th_core PUBLIC ${OpenCL_LIBRARIES})
target_include_directories(diskform4th_core PUBLIC ${ZSTD_INCLUDE_DIRS})
target_link_libraries(diskform4th_core PUBLIC ${ZSTD_LIBRARIES})
target_include_directories(diskform4th_core PUBLIC ${OPENSSL_INCLUDE_DIR})
target_link_libraries(diskform4th_core PUBLIC OpenSSL::Crypto)

if (CMAKE_SYSTEM_NAME MATCHES "Linux")
    target_link_libraries(diskform4th_core PUBLIC ${LIBURING_LIBRARIES})
    target_include_directories(diskform4th_core PUBLIC ${LIBURING_INCLUDE_DIRS})
endif()

# Linux Native CLI Executable
add_executable(diskform4th_cli src/cli/main.cpp)
target_link_libraries(diskform4th_cli PRIVATE diskform4th_core)
