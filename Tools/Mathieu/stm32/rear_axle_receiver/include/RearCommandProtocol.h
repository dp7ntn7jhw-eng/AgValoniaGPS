#pragma once

#include <Arduino.h>

struct RearCommand
{
    uint16_t seq = 0;
    uint32_t ms = 0;
    bool valid = false;
    int32_t xteMm = 0;
    int32_t steerCdeg = 0;
    uint16_t flags = 0;
    char track[32] = {0};
    char reason[48] = {0};
};

struct RearCommandParseResult
{
    bool ok = false;
    char error[64] = {0};
    RearCommand command;
};

uint16_t rearComputeChecksum(const char* payload);
RearCommandParseResult rearParseCommand(char* message);
