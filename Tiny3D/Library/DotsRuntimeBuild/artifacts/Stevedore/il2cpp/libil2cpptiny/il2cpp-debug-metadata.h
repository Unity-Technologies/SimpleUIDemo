#pragma once

typedef struct TinyMethod
{
    const char* fullName;
} TinyMethod;

typedef struct TinyStackFrameInfo
{
    const TinyMethod *method;
} TinyStackFrameInfo;
