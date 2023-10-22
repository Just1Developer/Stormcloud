#ifndef MEMORY_H
#define MEMORY_H

#include <stdlib.h>
#include "constants.h"

extern Bitboard** RookMoves;
extern Bitboard** BishopMoves;
extern Bitboard KnightMoves[64];
extern Bitboard KingMoves[64];

extern void allocateMagicTables();
extern void freeMagicTables();

#endif