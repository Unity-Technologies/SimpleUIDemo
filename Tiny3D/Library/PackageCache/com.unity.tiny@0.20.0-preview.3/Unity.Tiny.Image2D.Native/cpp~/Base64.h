#pragma once

#include <vector>
#include <string>
#include <cstring>

bool DecodeDataURIBase64(std::vector<uint8_t>& dest, std::string& mediaType, const char* src, size_t srclen);
bool DecodeBase64(std::vector<uint8_t>& dest, const char* src, size_t srclen);

void EncodeDataURIBase64(std::vector<char>& dest, const char* mediaType, const uint8_t* src, size_t srclen);
void EncodeBase64(std::vector<char>& dest, const uint8_t* src, size_t srclen, bool pad);
