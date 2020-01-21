#pragma once

typedef enum Baselib_FileIO_OpenShareFlags_PlatformDefined_t
{
    Baselib_FileIO_OpenShareFlags_PlatformDefined_AllowNone   = 0x0 * Baselib_FileIO_OpenShareFlags_PlatformDefined, // 0
    Baselib_FileIO_OpenShareFlags_PlatformDefined_AllowRead   = 0x1 * Baselib_FileIO_OpenShareFlags_PlatformDefined, // FILE_SHARE_READ
    Baselib_FileIO_OpenShareFlags_PlatformDefined_AllowWrite  = 0x2 * Baselib_FileIO_OpenShareFlags_PlatformDefined, // FILE_SHARE_WRITE
    Baselib_FileIO_OpenShareFlags_PlatformDefined_AllowDelete = 0x4 * Baselib_FileIO_OpenShareFlags_PlatformDefined, // FILE_SHARE_DELETE (this also allows others to rename)
} Baselib_FileIO_OpenShareFlags_PlatformDefined_t;
