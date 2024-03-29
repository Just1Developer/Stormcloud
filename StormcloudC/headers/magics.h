#ifndef MAGICS_H
#define MAGICS_H

#include "constants.h"

Bitboard RookMagics[64] = {
    0x8000c004201382ULL,		// size: 4096
    0x440200210044000ULL,		// size: 2048
    0x2080100080082000ULL,		// size: 2048
    0x20004400a001020ULL,		// size: 2048
    0x4600020020041089ULL,		// size: 2048
    0x1a00420001100814ULL,		// size: 2048
    0x200380120842200ULL,		// size: 2048
    0x4100008021000042ULL,		// size: 4096
    0x64800086a14008ULL,		// size: 2048
    0x404000201000ULL,		// size: 1024
    0x2080100020008aULL,		// size: 1024
    0x3041800800801002ULL,		// size: 1024
    0x41800401800801ULL,		// size: 1024
    0x20808002008400ULL,		// size: 1024
    0x4000421020850ULL,		// size: 1024
    0x600010a008044ULL,		// size: 2048
    0x80010020408104ULL,		// size: 2048
    0x110084000482000ULL,		// size: 1024
    0x2010002000280400ULL,		// size: 1024
    0x20120042000820ULL,		// size: 1024
    0x4921010004100800ULL,		// size: 1024
    0x2008004008002ULL,		// size: 1024
    0x600240042411850ULL,		// size: 1024
    0x1020009118244ULL,		// size: 2048
    0x11205020023c080ULL,		// size: 2048
    0x500240022000ULL,		// size: 1024
    0x10008180112000ULL,		// size: 1024
    0x402200900100101ULL,		// size: 1024
    0x810110100040800ULL,		// size: 1024
    0x14008080040200ULL,		// size: 1024
    0x1880020400282110ULL,		// size: 1024
    0x108200204104ULL,		// size: 2048
    0x20e0204000800080ULL,		// size: 2048
    0x40002000808040ULL,		// size: 1024
    0x402004082001024ULL,		// size: 1024
    0x140100080800800ULL,		// size: 1024
    0x210800800800401ULL,		// size: 1024
    0x2801401800200ULL,		// size: 1024
    0x8a1100804000201ULL,		// size: 1024
    0x15001088200004cULL,		// size: 2048
    0x852258340028003ULL,		// size: 2048
    0x101008200220044ULL,		// size: 1024
    0x18c8402200820010ULL,		// size: 1024
    0x1020100101000aULL,		// size: 1024
    0x40801010010ULL,		// size: 1024
    0x10600a811220004ULL,		// size: 1024
    0x20010208040010ULL,		// size: 1024
    0x400100a044060005ULL,		// size: 2048
    0x8508008210100ULL,		// size: 2048
    0xa004080210200ULL,		// size: 1024
    0x888200049001100ULL,		// size: 1024
    0x201000220b0100ULL,		// size: 1024
    0x28010009041100ULL,		// size: 1024
    0x800400020080ULL,		// size: 1024
    0x800c20178100c00ULL,		// size: 1024
    0x2005000080420100ULL,		// size: 2048
    0x6002008048201106ULL,		// size: 4096
    0x10400080110021ULL,		// size: 2048
    0x2030402005001009ULL,		// size: 2048
    0x1080200810000501ULL,		// size: 2048
    0xc1001002080005ULL,		// size: 2048
    0x200a000130880422ULL,		// size: 2048
    0x1000100108022084ULL,		// size: 2048
    0x6298002040840d02ULL,		// size: 4096
};

Bitboard BishopMagics[64] = {
    0x4048100414404200ULL,           // size: 64
    0x5110112124208010ULL,           // size: 32
    0xb1411202042108ULL,             // size: 32
    0x4424414180080000ULL,           // size: 32
    0x1494230601400001ULL,           // size: 32
    0x2c0900420408000ULL,            // size: 32
    0x7180980110102001ULL,           // size: 32
    0x81d10c01044008ULL,             // size: 64
    0x60144c410240101ULL,            // size: 32
    0x4c800801c1040100ULL,           // size: 32
    0x40410408e204182ULL,            // size: 32
    0x24080865000001ULL,             // size: 32
    0x2420210100840ULL,              // size: 32
    0x8020130890200ULL,              // size: 32
    0x11080104a220ULL,               // size: 32
    0x10108208010480ULL,             // size: 32
    0x108421002100420ULL,            // size: 32
    0x21a4010244042400ULL,           // size: 32
    0x920811004868008ULL,            // size: 128
    0x4000241020200ULL,              // size: 128
    0x42a820400a06000ULL,            // size: 128
    0x100040020110212aULL,           // size: 128
    0x750804402119010ULL,            // size: 32
    0x5120280413000ULL,              // size: 32
    0x4002091311901000ULL,           // size: 32
    0x4002a00408010410ULL,           // size: 32
    0x2002280004080024ULL,           // size: 128
    0x4040808208020082ULL,           // size: 512
    0x10840200802004ULL,             // size: 512
    0x410122001040100ULL,            // size: 128
    0x2049084148802ULL,              // size: 32
    0x211010800208800ULL,            // size: 32
    0x20a848040040041aULL,           // size: 32
    0x42020200103008ULL,             // size: 32
    0x3044a29010080020ULL,           // size: 128
    0x204a004040140102ULL,           // size: 512
    0x10008200202200ULL,             // size: 512
    0x4210208080380a00ULL,           // size: 128
    0x1440808404043cULL,             // size: 32
    0x802420200284642ULL,            // size: 32
    0x88e2012402004ULL,              // size: 32
    0x81009010440480ULL,             // size: 32
    0x40a0082041010ULL,              // size: 128
    0x12109c4200801805ULL,           // size: 128
    0x2000080104020040ULL,           // size: 128
    0x4008092008100ULL,              // size: 128
    0x209411c404095100ULL,           // size: 32
    0x404108082011900ULL,            // size: 32
    0x84008804120201ULL,             // size: 32
    0x200410848c088ULL,              // size: 32
    0x890104404040080ULL,            // size: 32
    0x112100104880080ULL,            // size: 32
    0x2081042020040ULL,              // size: 32
    0x1100290010002ULL,              // size: 32
    0x20041010c10081ULL,             // size: 32
    0x82044c102002100ULL,            // size: 32
    0x160a010048020802ULL,           // size: 64
    0xc08084a08140210ULL,            // size: 32
    0x4004100200840408ULL,           // size: 32
    0x4000831010460800ULL,           // size: 32
    0x480400004050404ULL,            // size: 32
    0x9002004504084ULL,              // size: 32
    0x8411014210c0410ULL,            // size: 32
    0x1064042408020014ULL,           // size: 64
};

#endif