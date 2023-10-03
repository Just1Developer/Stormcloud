#include "headers/memory.h"
#include <stdio.h>

uint64** RookMoves;
uint64** BishopMoves;

void allocateMagicTables()
{
    RookMoves = (uint64**) malloc(BOARD_SIZE * sizeof(uint64*));
    BishopMoves = (uint64**) malloc(BOARD_SIZE * sizeof(uint64*));

    for(short i = 0; i < BOARD_SIZE; ++i)
    {
        RookMoves[i] = (uint64*) malloc(RookArraySizes[i] * sizeof(uint64));
        BishopMoves[i] = (uint64*) malloc(BishopArraySizes[i] * sizeof(uint64));
    }

    printf("method allocate called");
}

void freeMagicTables()
{
    for(short i = 0; i < BOARD_SIZE; ++i)
    {
        free(RookMoves[i]);
        free(BishopMoves[i]);
    }
    free(RookMoves);
    free(BishopMoves);
    printf("method free called");
}