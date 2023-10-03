#include <stdio.h>
#include "headers/memory.h"

// for error undefined reference to WinMain
#define NOMINMAX
#include <windows.h>


/* GPT4 Generated
#include <psapi.h>
// Link against Psapi.lib
#pragma comment(lib, "psapi.lib")
void printMemoryUsage() {
    HANDLE hProcess;
    PROCESS_MEMORY_COUNTERS pmc;

    // Get the handle to the current process.
    hProcess = GetCurrentProcess();

    // Retrieve the memory usage information.
    if (GetProcessMemoryInfo(hProcess, &pmc, sizeof(pmc))) {
        // Print the memory usage information.
        printf("PageFaultCount: %u\n", pmc.PageFaultCount);
        printf("PeakWorkingSetSize: %u\n", pmc.PeakWorkingSetSize);
        printf("WorkingSetSize: %u\n", pmc.WorkingSetSize);
        printf("QuotaPeakPagedPoolUsage: %u\n", pmc.QuotaPeakPagedPoolUsage);
        printf("QuotaPagedPoolUsage: %u\n", pmc.QuotaPagedPoolUsage);
        printf("QuotaPeakNonPagedPoolUsage: %u\n", pmc.QuotaPeakNonPagedPoolUsage);
        printf("QuotaNonPagedPoolUsage: %u\n", pmc.QuotaNonPagedPoolUsage);
        printf("PagefileUsage: %u\n", pmc.PagefileUsage);
        printf("PeakPagefileUsage: %u\n", pmc.PeakPagefileUsage);
    }

    // Close the handle.
    CloseHandle(hProcess);
}
*/


// Why is there no change in memory before/after allocate calls?
int main()
{
    printf("Hello World!\n");
    printMemoryUsage();
    getchar();
    allocateMagicTables();
    printf("Allocated magic tables, rooks: %d\n", sizeof(RookMoves));
    printMemoryUsage();
    getchar();
    freeMagicTables();
    printf("Freed all memory.\n");
    printMemoryUsage();
    getchar();
}