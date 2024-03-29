#ifndef TYPEDEFS_H
#define TYPEDEFS_H

#define BOARD_SIZE 64
/*
#define MAX_FILE_VALUE 7
#define MAX_RANK_VALUE 7
#define BITMASK_FILE 0b111
#define BITMASK_RANK 0b111000

#define GET_RANK_OF_SQUARE (x) (x >> 3)
#define GET_RANK_OF_SQUARE (x) (x & BITMASK_FILE)
*/

typedef unsigned long long Bitboard;

static short RookBits[64] = {
    12, 11, 11, 11, 11, 11, 11, 12,
    11, 10, 10, 10, 10, 10, 10, 11,
    11, 10, 10, 10, 10, 10, 10, 11,
    11, 10, 10, 10, 10, 10, 10, 11,
    11, 10, 10, 10, 10, 10, 10, 11,
    11, 10, 10, 10, 10, 10, 10, 11,
    11, 10, 10, 10, 10, 10, 10, 11,
    12, 11, 11, 11, 11, 11, 11, 12,
};
static short RookArraySizes[64] = {
    4096, 2048, 2048, 2048, 2048, 2048, 2048, 4096,
    2048, 1024, 1024, 1024, 1024, 1024, 1024, 2048,
    2048, 1024, 1024, 1024, 1024, 1024, 1024, 2048,
    2048, 1024, 1024, 1024, 1024, 1024, 1024, 2048,
    2048, 1024, 1024, 1024, 1024, 1024, 1024, 2048,
    2048, 1024, 1024, 1024, 1024, 1024, 1024, 2048,
    2048, 1024, 1024, 1024, 1024, 1024, 1024, 2048,
    4096, 2048, 2048, 2048, 2048, 2048, 2048, 4096,
};

static short BishopBits[64] = {
    6, 5, 5, 5, 5, 5, 5, 6,
    5, 5, 5, 5, 5, 5, 5, 5,
    5, 5, 7, 7, 7, 7, 5, 5,
    5, 5, 7, 9, 9, 7, 5, 5,
    5, 5, 7, 9, 9, 7, 5, 5,
    5, 5, 7, 7, 7, 7, 5, 5,
    5, 5, 5, 5, 5, 5, 5, 5,
    6, 5, 5, 5, 5, 5, 5, 6
};
static short BishopArraySizes[64] = {
    64, 32, 32, 32, 32, 32, 32, 64,
    32, 32, 32, 32, 32, 32, 32, 32,
    32, 32, 128, 128, 128, 128, 32, 32,
    32, 32, 128, 512, 512, 128, 32, 32,
    32, 32, 128, 512, 512, 128, 32, 32,
    32, 32, 128, 128, 128, 128, 32, 32,
    32, 32, 32, 32, 32, 32, 32, 32,
    64, 32, 32, 32, 32, 32, 32, 64
};

#endif