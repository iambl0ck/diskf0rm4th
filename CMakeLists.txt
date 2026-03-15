cmake_minimum_required(VERSION 3.10)
project(diskform4th CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED True)

# Platform specific configurations
if (CMAKE_SYSTEM_NAME MATCHES "Linux")
    find_package(PkgConfig REQUIRED)
    pkg_check_modules(LIBURING REQUIRED liburing)
endif()

# Core library
add_library(diskform4th_core STATIC src/core/io_buffer.cpp)
target_include_directories(diskform4th_core PUBLIC src/core)

if (CMAKE_SYSTEM_NAME MATCHES "Linux")
    target_link_libraries(diskform4th_core PUBLIC ${LIBURING_LIBRARIES})
    target_include_directories(diskform4th_core PUBLIC ${LIBURING_INCLUDE_DIRS})
endif()

# A simple test executable to check compilation
add_executable(core_test src/core/io_buffer.cpp)
target_include_directories(core_test PUBLIC src/core)
if (CMAKE_SYSTEM_NAME MATCHES "Linux")
    target_link_libraries(core_test PUBLIC ${LIBURING_LIBRARIES})
endif()
# Add a dummy main for core_test to compile
file(WRITE ${CMAKE_BINARY_DIR}/dummy_main.cpp "int main() { return 0; }")
target_sources(core_test PRIVATE ${CMAKE_BINARY_DIR}/dummy_main.cpp)
