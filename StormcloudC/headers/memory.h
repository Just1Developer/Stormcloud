#ifndef MEMORY_H
#define MEMORY_H

#include <stdlib.h>
#include "constants.h"

extern uint64** RookMoves;
extern uint64** BishopMoves;
extern uint64 KnightMoves[64];
extern uint64 KingMoves[64];

extern void allocateMagicTables();
extern void freeMagicTables();

#endif