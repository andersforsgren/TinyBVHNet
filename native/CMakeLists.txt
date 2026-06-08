cmake_minimum_required(VERSION 3.20)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

project(TinyBVHNetNative LANGUAGES CXX)

# Include the TinyBVH header
set(TinyBVH_DIR "${CMAKE_CURRENT_SOURCE_DIR}/../external/tinybvh")

# Build as a shared library (DLL on Windows, .so on Linux, .dylib on macOS)
# Name must differ from the managed assembly (TinyBVHNet.dll) to avoid collision.
add_library(TinyBVHNetNative SHARED
    TinyBVH_c.cpp
)

target_include_directories(TinyBVHNetNative PRIVATE ${TinyBVH_DIR})

# Enable double-precision BVH support (BVH_Double)
target_compile_definitions(TinyBVHNetNative PRIVATE DOUBLE_PRECISION_SUPPORT)

if(MSVC)
    # Export all symbols on Windows
    set_target_properties(TinyBVHNetNative PROPERTIES
        WINDOWS_EXPORT_ALL_SYMBOLS TRUE
    )
    # Use static runtime (/MT) to avoid dependency on MSVCP140.dll / VCRUNTIME140.dll
    set_property(TARGET TinyBVHNetNative PROPERTY MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")
    # SIMD flags: AVX2 on x64, no arch flag on ARM64 (NEON used via arm_neon.h)
    # Check both host processor and generator platform (for cross-compilation)
    if(CMAKE_GENERATOR_PLATFORM MATCHES "ARM64|arm64" OR CMAKE_VS_PLATFORM_NAME MATCHES "ARM64|arm64" OR CMAKE_SYSTEM_PROCESSOR MATCHES "ARM64|arm64|aarch64")
        target_compile_options(TinyBVHNetNative PRIVATE /O2)
    else()
        target_compile_options(TinyBVHNetNative PRIVATE /O2 /arch:AVX2)
    endif()
else()
    target_compile_options(TinyBVHNetNative PRIVATE -O3)
endif()

# Install targets
install(TARGETS TinyBVHNetNative
    RUNTIME DESTINATION bin
    LIBRARY DESTINATION lib
    ARCHIVE DESTINATION lib
)
