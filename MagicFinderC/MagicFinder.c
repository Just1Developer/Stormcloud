#include <stdio.h>
#include <stdlib.h>


// Define the use of 32-bit multiplications for some calculations.
//#define USE_32_BIT_MULTIPLICATIONS

typedef unsigned long long uint64;

// Generate a random 64-bit unsigned integer.
uint64 random_uint64() {
  uint64 u1, u2, u3, u4;
  u1 = (uint64)(random()) & 0xFFFF; // Take the lower 16 bits.
  u2 = (uint64)(random()) & 0xFFFF;
  u3 = (uint64)(random()) & 0xFFFF;
  u4 = (uint64)(random()) & 0xFFFF;
  return u1 | (u2 << 16) | (u3 << 32) | (u4 << 48);
}

//* *memory[i] == *(memory + i)
int containsOrAdd(int num, int *memory)
{
    int firstEmpty = -1;
    for (int i = 0; i < 64; i++)
    {
        if(firstEmpty == -1 && *(memory + i) == -1) { firstEmpty = i; continue; }
        if(*(memory + i) == num) return -1;
    }
    if(firstEmpty == -1) return 0;
    *(memory + firstEmpty) = num;
    return 1;
}

uint64 GenerateRandomNumber(int bitsSet)
{
    uint64 result = 0;
    // Acts as a Set in C#
    int usedBits[64];
    // Fill array with -1, declare as unused in logic
    for (int i = 0; i < 64; i++)
    {
        usedBits[i] = -1;
    }

    for (int i = 0; i < bitsSet; ++i)
    {
        int bitPos;
        do
        {
            bitPos = random() & 0b111111;
        } while (containsOrAdd(bitPos, usedBits) == -1);

        result |= (1UL << bitPos);
    }

    return result;
}
//*/

// Generate a random 64-bit integer with fewer set bits.
uint64 random_uint64_fewbits() {
  return random_uint64() & random_uint64() & random_uint64();
}

// Count the number of set bits (1's) in a 64-bit integer.
int count_1s(uint64 b) {
  int r;
  for (r = 0; b; r++, b &= b - 1);
  return r;
}

// A pre-computed table to determine which bit is set.
const int BitTable[64] = {
  63, 30, 3, 32, 25, 41, 22, 33, 15, 50, 42, 13, 11, 53, 19, 34, 61, 29, 2,
  51, 21, 43, 45, 10, 18, 47, 1, 54, 9, 57, 0, 35, 62, 31, 40, 4, 49, 5, 52,
  26, 60, 6, 23, 44, 46, 27, 56, 16, 7, 39, 48, 24, 59, 14, 12, 55, 38, 28,
  58, 20, 37, 17, 36, 8
};

// Pop the least significant set bit and return its index.
int pop_1st_bit(uint64 *bb) {
  uint64 b = *bb ^ (*bb - 1);
  unsigned int fold = (unsigned) ((b & 0xffffffff) ^ (b >> 32));
  *bb &= (*bb - 1);
  return BitTable[(fold * 0x783a9b23) >> 26];
}

// Convert an index to a 64-bit integer based on the bits and mask.
uint64 index_to_uint64(int index, int bits, uint64 m) {
  int i, j;
  uint64 result = 0ULL;
  for(i = 0; i < bits; i++) {
    j = pop_1st_bit(&m);
    if(index & (1 << i)) result |= (1ULL << j);
  }
  return result;
}

// Generate rook mask for a given square on the chessboard.
uint64 rmask(int sq) {
  uint64 result = 0ULL;
  int rk = sq/8, fl = sq%8, r, f;
  for(r = rk+1; r <= 6; r++) result |= (1ULL << (fl + r*8));
  for(r = rk-1; r >= 1; r--) result |= (1ULL << (fl + r*8));
  for(f = fl+1; f <= 6; f++) result |= (1ULL << (f + rk*8));
  for(f = fl-1; f >= 1; f--) result |= (1ULL << (f + rk*8));
  return result;
}

// Generate bishop mask for a given square on the chessboard.
uint64 bmask(int sq) {
  uint64 result = 0ULL;
  int rk = sq/8, fl = sq%8, r, f;
  for(r=rk+1, f=fl+1; r<=6 && f<=6; r++, f++) result |= (1ULL << (f + r*8));
  for(r=rk+1, f=fl-1; r<=6 && f>=1; r++, f--) result |= (1ULL << (f + r*8));
  for(r=rk-1, f=fl+1; r>=1 && f<=6; r--, f++) result |= (1ULL << (f + r*8));
  for(r=rk-1, f=fl-1; r>=1 && f>=1; r--, f--) result |= (1ULL << (f + r*8));
  return result;
}

// Generate rook attacks for a given square and block configuration.
uint64 ratt(int sq, uint64 block) {
  uint64 result = 0ULL;
  int rk = sq/8, fl = sq%8, r, f;
  for(r = rk+1; r <= 7; r++) {
    result |= (1ULL << (fl + r*8));
    if(block & (1ULL << (fl + r*8))) break;
  }
  for(r = rk-1; r >= 0; r--) {
    result |= (1ULL << (fl + r*8));
    if(block & (1ULL << (fl + r*8))) break;
  }
  for(f = fl+1; f <= 7; f++) {
    result |= (1ULL << (f + rk*8));
    if(block & (1ULL << (f + rk*8))) break;
  }
  for(f = fl-1; f >= 0; f--) {
    result |= (1ULL << (f + rk*8));
    if(block & (1ULL << (f + rk*8))) break;
  }
  return result;
}

// Generate bishop attacks for a given square and block configuration.
uint64 batt(int sq, uint64 block) {
  uint64 result = 0ULL;
  int rk = sq/8, fl = sq%8, r, f;
  for(r = rk+1, f = fl+1; r <= 7 && f <= 7; r++, f++) {
    result |= (1ULL << (f + r*8));
    if(block & (1ULL << (f + r * 8))) break;
  }
  for(r = rk+1, f = fl-1; r <= 7 && f >= 0; r++, f--) {
    result |= (1ULL << (f + r*8));
    if(block & (1ULL << (f + r * 8))) break;
  }
  for(r = rk-1, f = fl+1; r >= 0 && f <= 7; r--, f++) {
    result |= (1ULL << (f + r*8));
    if(block & (1ULL << (f + r * 8))) break;
  }
  for(r = rk-1, f = fl-1; r >= 0 && f >= 0; r--, f--) {
    result |= (1ULL << (f + r*8));
    if(block & (1ULL << (f + r * 8))) break;
  }
  return result;
}


// Transform the board configuration with a magic number.
int transform(uint64 b, uint64 magic, int bits) {
#if defined(USE_32_BIT_MULTIPLICATIONS)
  return
    (unsigned)((int)b*(int)magic ^ (int)(b>>32)*(int)(magic>>32)) >> (32-bits);
#else
  return (int)((b * magic) >> (64 - bits));
#endif
}

// Find a magic number for a given square, number of bits, and piece (rook/bishop).
uint64 find_magic(int sq, int m, int bishop) {
  uint64 mask, blockers[4096], attacks[4096], used[4096], magic;
  int i, hash, k, n, fail;

  mask = bishop? bmask(sq) : rmask(sq);
  n = count_1s(mask);

  for(i = 0; i < (1 << n); i++) {
    // Set blockers
    blockers[i] = index_to_uint64(i, n, mask);
    attacks[i] = bishop? batt(sq, blockers[i]) : ratt(sq, blockers[i]);
  }
  for(k = 0; k < 100000000; k++) {
    magic = random_uint64_fewbits();
    if(count_1s((mask * magic) & 0xFF00000000000000ULL) < 6) continue;
    for(i = 0; i < 4096; i++) used[i] = 0ULL; // Reset used
    for(i = 0, fail = 0; !fail && i < (1 << n); i++) {
      hash = transform(blockers[i], magic, m);
      if(used[hash] == 0ULL) used[hash] = attacks[i];
      else if(used[hash] != attacks[i]) fail = 1;
    }
    if(!fail) return magic;
  }
  printf("***Failed***\n");
  return 0ULL;
}

// Number of relevant bits for rooks on each square.
int RBits[64] = {
  12, 11, 11, 11, 11, 11, 11, 12,
  11, 10, 10, 10, 10, 10, 10, 11,
  11, 10, 10, 10, 10, 10, 10, 11,
  11, 10, 10, 10, 10, 10, 10, 11,
  11, 10, 10, 10, 10, 10, 10, 11,
  11, 10, 10, 10, 10, 10, 10, 11,
  11, 10, 10, 10, 10, 10, 10, 11,
  12, 11, 11, 11, 11, 11, 11, 12
};

// Number of relevant bits for bishops on each square.
int BBits[64] = {
  6, 5, 5, 5, 5, 5, 5, 6,
  5, 5, 5, 5, 5, 5, 5, 5,
  5, 5, 7, 7, 7, 7, 5, 5,
  5, 5, 7, 9, 9, 7, 5, 5,
  5, 5, 7, 9, 9, 7, 5, 5,
  5, 5, 7, 7, 7, 7, 5, 5,
  5, 5, 5, 5, 5, 5, 5, 5,
  6, 5, 5, 5, 5, 5, 5, 6
};

/*
int main() {
  int square;

  printf("const uint64 RMagic[64] = {\n");
  for(square = 0; square < 64; square++)
    printf("  0x%llxULL,\n", find_magic(square, RBits[square], 0));
  printf("};\n\n");

  printf("const uint64 BMagic[64] = {\n");
  for(square = 0; square < 64; square++)
    printf("  0x%llxULL,\n", find_magic(square, BBits[square], 1));
  printf("};\n\n");

  return 0;
}
*/

// $ ./program_name <SEED_NUMBER>
int main(int argc, char *argv[]) {
  int square;

  // Check if the random seed is provided, if so, use it.
  if (argc > 1) {
    long seed = strtol(argv[1], NULL, 10);
    srandom(seed);
  }

  printf("const uint64 RMagic[64] = {\n");
  for (square = 0; square < 64; square++)
    printf("  0x%llxULL,\n", find_magic(square, RBits[square], 0));
  printf("};\n\n");

  printf("const uint64 BMagic[64] = {\n");
  for (square = 0; square < 64; square++)
    printf("  0x%llxULL,\n", find_magic(square, BBits[square], 1));
  printf("};\n\n");

  return 0;
}