#pragma once

typedef enum Baselib_FileIO_CreatePermissionsFlags_PlatformDefined_t
{
    Baselib_FileIO_CreatePermissionsFlags_PlatformDefined_Posix_Owner_Read     = 0400 * Baselib_FileIO_CreatePermissionsFlags_PlatformDefined, // S_IRUSR
    Baselib_FileIO_CreatePermissionsFlags_PlatformDefined_Posix_Owner_Write    = 0200 * Baselib_FileIO_CreatePermissionsFlags_PlatformDefined, // S_IWUSR
    Baselib_FileIO_CreatePermissionsFlags_PlatformDefined_Posix_Owner_Execute  = 0100 * Baselib_FileIO_CreatePermissionsFlags_PlatformDefined, // S_IXUSR
    Baselib_FileIO_CreatePermissionsFlags_PlatformDefined_Posix_Group_Read     = 0040 * Baselib_FileIO_CreatePermissionsFlags_PlatformDefined, // S_IRGRP
    Baselib_FileIO_CreatePermissionsFlags_PlatformDefined_Posix_Group_Write    = 0020 * Baselib_FileIO_CreatePermissionsFlags_PlatformDefined, // S_IWGRP
    Baselib_FileIO_CreatePermissionsFlags_PlatformDefined_Posix_Group_Execute  = 0010 * Baselib_FileIO_CreatePermissionsFlags_PlatformDefined, // S_IXGRP
    Baselib_FileIO_CreatePermissionsFlags_PlatformDefined_Posix_Others_Read    = 0004 * Baselib_FileIO_CreatePermissionsFlags_PlatformDefined, // S_IROTH
    Baselib_FileIO_CreatePermissionsFlags_PlatformDefined_Posix_Others_Write   = 0002 * Baselib_FileIO_CreatePermissionsFlags_PlatformDefined, // S_IWOTH
    Baselib_FileIO_CreatePermissionsFlags_PlatformDefined_Posix_Others_Execute = 0001 * Baselib_FileIO_CreatePermissionsFlags_PlatformDefined  // S_IXOTH
} Baselib_FileIO_CreatePermissionsFlags_PlatformDefined_t;
