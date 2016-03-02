#include "PCM_Win/stdafx.h"

/*
Copyright (c) 2009-2012, Intel Corporation
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
* Neither the name of Intel Corporation nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
// written by Roman Dementiev,
//            Thomas Willhalm,
//            Patrick Ungerer


/*!     \file cpucounterstest.cpp
\brief Example of using CPU counters: implements a simple performance counter monitoring utility
*/
#define HACK_TO_REMOVE_DUPLICATE_ERROR 
#include <iostream>
#ifdef _MSC_VER
#pragma warning(disable : 4996) // for sprintf
#include <windows.h>
#include "PCM_Win/windriver.h"
#else
#include <unistd.h>
#include <signal.h>
#endif
#include <math.h>
#include <iomanip>
#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <string>
#include <assert.h>
#include "cpucounters.h"
#include "utils.h"

#define SIZE (10000000)
#define DELAY 1 // in milliseconds

using namespace std;

template <class IntType>
double float_format(IntType n)
{
	return double(n) / 1024 / 1024;
}

std::string temp_format(int32 t)
{
	char buffer[1024];
	if (t == PCM_INVALID_THERMAL_HEADROOM)
		return "N/A";

	sprintf(buffer, "%2d", t);
	return buffer;
}

void print_help(char * prog_name)
{
#ifdef _MSC_VER
	cout << " Usage: pcm <delay>|\"external_program parameters\"|--help|--uninstallDriver|--installDriver <other options>" << endl;
#else
	cout << " Usage: pcm <delay>|\"external_program parameters\"|--help <other options>" << endl;
#endif
	cout << endl;
	cout << " \n Other options:" << endl;
	cout << " -nc or --nocores or /nc => hides core related output" << endl;
	cout << " -ns or --nosockets or /ns => hides socket related output" << endl;
	cout << " -nsys or --nosystem or /nsys => hides system related output" << endl;
	cout << " -csv or /csv => print compact csv format" << endl;
	cout << " Example:  pcm.x 1 -nc -ns " << endl;
	cout << endl;
}



void print_csv_header(PCM * m,
	const int cpu_model,
	const bool show_core_output,
	const bool show_socket_output,
	const bool show_system_output
	)
{
	// print first header line
	cout << "\nSYSTEM;SYSTEM;";
	if (show_system_output)
	{
		if (cpu_model != PCM::ATOM)
			cout << "SYSTEM;SYSTEM;SYSTEM;SYSTEM;SYSTEM;SYSTEM;SYSTEM;SYSTEM;SYSTEM;SYSTEM;SYSTEM;";
		else
			cout << "SYSTEM;SYSTEM;SYSTEM;SYSTEM;SYSTEM;";


		cout << "SYSTEM;SYSTEM;SYSTEM;SYSTEM;SYSTEM;SYSTEM;SYSTEM;";
		if (m->getNumSockets() > 1) // QPI info only for multi socket systems
			cout << "SYSTEM;SYSTEM;";
		if (m->qpiUtilizationMetricsAvailable())
			cout << "SYSTEM;";
	}

	if (show_core_output)
	{
		for (uint32 i = 0; i < m->getNumCores(); ++i)
		{
			if (cpu_model == PCM::ATOM){
				for (uint32 j = 0; j < 6; ++j)
					cout << "C" << i << "@S" << m->getSocketId(i) << ";";
			}
			else{
				for (uint32 j = 0; j < 10; ++j)
					cout << "C" << i << "@S" << m->getSocketId(i) << ";";
			}

		}
	}

	// print second header line
	cout << "\nTIME;";
	if (show_system_output)
	{
		if (cpu_model != PCM::ATOM)
			cout << "EXEC;IPC;FREQ;AFREQ;L3MISS;L2MISS;L3HIT;L2HIT;L3CLK;L2CLK;READ;WRITE;";
		else
			cout << "EXEC;IPC;FREQ;L2MISS;L2HIT;";


		cout << "INST;ACYC;TICKS;IPC;INST;MAXIPC;";
		if (m->getNumSockets() > 1) // QPI info only for multi socket systems
			cout << "TotalQPIin;QPItoMC;";
		if (m->outgoingQPITrafficMetricsAvailable())
			cout << "TotalQPIout;";
	}




	if (show_core_output)
	{
		for (uint32 i = 0; i < m->getNumCores(); ++i)
		{
			if (cpu_model == PCM::ATOM)
				cout << "EXEC;IPC;FREQ;L2MISS;L2HIT;";
			else
				cout << "EXEC;IPC;FREQ;AFREQ;L3MISS;L2MISS;L3HIT;L2HIT;L3CLK;L2CLK;";

		}

	}

}

long milli = 0;
long prevc = 0;
long prevs = -1;
bool start = false;

void print_csv(PCM * m,
	const std::vector<CoreCounterState> & cstates1,
	const std::vector<CoreCounterState> & cstates2,
	const std::vector<SocketCounterState> & sktstate1,
	const std::vector<SocketCounterState> & sktstate2,
	const SystemCounterState& sstate1,
	const SystemCounterState& sstate2,
	const int cpu_model,
	const bool show_core_output,
	const bool show_socket_output,
	const bool show_system_output
	)
{
	time_t t = time(NULL);
	tm *tt = localtime(&t);
	cout.precision(3);
	// << 1900+tt->tm_year << '-' << 1+tt->tm_mon << '-' << tt->tm_mday << ';'


	long ctime = clock();
	if (prevs != tt->tm_sec){
		milli = 0;
		if (prevs != -1)
			start = true;
	}
	else{
		milli += ctime - prevc;
	}
	prevs = tt->tm_sec;
	prevc = ctime;

	if (!start)
		return;

	cout << "\n" << tt->tm_hour << ':' << tt->tm_min << ':' << tt->tm_sec << ':' << milli << ';';

	if (show_system_output)
	{
		if (cpu_model != PCM::ATOM)
		{
			cout << getExecUsage(sstate1, sstate2) <<
				';' << getIPC(sstate1, sstate2) <<
				';' << getRelativeFrequency(sstate1, sstate2) <<
				';' << getActiveRelativeFrequency(sstate1, sstate2) <<
				';' << getL3CacheMisses(sstate1, sstate2) << // float_format()
				';' << getL2CacheMisses(sstate1, sstate2) <<
				';' << getL3CacheHits(sstate1, sstate2) <<
				';' << getL2CacheHits(sstate1, sstate2) <<
				';' << getCyclesLostDueL3CacheMisses(sstate1, sstate2) <<
				';' << getCyclesLostDueL2CacheMisses(sstate1, sstate2) <<
				';';


			if (!(m->memoryTrafficMetricsAvailable()))
				cout << "N/A;N/A;";
			else
				cout << getBytesReadFromMC(sstate1, sstate2) / double(1024ULL * 1024ULL * 1024ULL) <<
				';' << getBytesWrittenToMC(sstate1, sstate2) / double(1024ULL * 1024ULL * 1024ULL) << ';';
		}
		else
			cout << getExecUsage(sstate1, sstate2) <<
			';' << getIPC(sstate1, sstate2) <<
			';' << getRelativeFrequency(sstate1, sstate2) <<
			';' << float_format(getL2CacheMisses(sstate1, sstate2)) <<
			';' << getL2CacheHitRatio(sstate1, sstate2) <<
			';';

		cout << getInstructionsRetired(sstate1, sstate2) << ";"
			<< getCycles(sstate1, sstate2) << ";"
			<< getInvariantTSC(cstates1[0], cstates2[0]) << ";"
			<< getCoreIPC(sstate1, sstate2) << ";"
			<< getTotalExecUsage(sstate1, sstate2) << ";"
			<< m->getMaxIPC() << ";";

		if (m->getNumSockets() > 1) // QPI info only for multi socket systems
			cout << float_format(getAllIncomingQPILinkBytes(sstate1, sstate2)) << ";"
			<< getQPItoMCTrafficRatio(sstate1, sstate2) << ";";
		if (m->outgoingQPITrafficMetricsAvailable())
			cout << float_format(getAllOutgoingQPILinkBytes(sstate1, sstate2)) << ";";


	}


	if (show_core_output)
	{
		for (uint32 i = 0; i < m->getNumCores(); ++i)
		{
			if (cpu_model != PCM::ATOM)
				cout << getExecUsage(cstates1[i], cstates2[i]) <<
				';' << getIPC(cstates1[i], cstates2[i]) <<
				';' << getRelativeFrequency(cstates1[i], cstates2[i]) <<
				';' << getActiveRelativeFrequency(cstates1[i], cstates2[i]) <<
				';' << getL3CacheMisses(cstates1[i], cstates2[i]) <<
				';' << getL2CacheMisses(cstates1[i], cstates2[i]) <<
				';' << getL3CacheHits(cstates1[i], cstates2[i]) <<
				';' << getL2CacheHits(cstates1[i], cstates2[i]) <<
				';' << getCyclesLostDueL3CacheMisses(cstates1[i], cstates2[i]) <<
				';' << getCyclesLostDueL2CacheMisses(cstates1[i], cstates2[i]) <<
				';' << getL2CoherencyMisses(cstates1[i], cstates2[i]) <<
				';' << getL1CoherencyMisses(cstates1[i], cstates2[i]) <<
				';';
			else
				cout << getExecUsage(cstates1[i], cstates2[i]) <<
				';' << getIPC(cstates1[i], cstates2[i]) <<
				';' << getRelativeFrequency(cstates1[i], cstates2[i]) <<
				';' << float_format(getL2CacheMisses(cstates1[i], cstates2[i])) <<
				';' << getL2CacheHitRatio(cstates1[i], cstates2[i]) <<
				';' << temp_format(cstates2[i].getThermalHeadroom()) <<
				';';

		}
	}

}


void print_harvester(PCM * m, bool tlbMode, uint64 duration,
	const std::vector<CoreCounterState> & cstates1,
	const std::vector<CoreCounterState> & cstates2,
	const std::vector<SocketCounterState> & sktstate1,
	const std::vector<SocketCounterState> & sktstate2,
	const SystemCounterState& sstate1,
	const SystemCounterState& sstate2,
	const int cpu_model
	)
{
	time_t t = time(NULL);
	tm *tt = localtime(&t);
	cout.precision(3);

	long ctime = clock();
	if (prevs != tt->tm_sec){
		milli = 0;
		if (prevs != -1)
			start = true;
	}
	else{
		milli += ctime - prevc;
	}
	prevs = tt->tm_sec;
	prevc = ctime;

	if (!start)
		return;

	cout << "\n" << tt->tm_hour << ':' << tt->tm_min << ':' << tt->tm_sec << ':' << milli << ';' << duration << ';';
	cout << (tlbMode ? "TLB" : "CACHE") ;

	// For each core we have
	for (uint32 i = 0; i < m->getNumCores(); ++i)
	{
		if (tlbMode)
		{
			// Get our TLB misses
			cout << ';' << getIPC(cstates1[i], cstates2[i]) <<
					';' << getCycles(cstates1[i], cstates2[i]) <<
					';' << getEvent0(cstates1[i], cstates2[i]) <<
					';' << getCyclesLostDueTLBMisses(cstates1[i], cstates2[i]);
		}
		else
		{
			// Default events we need to get per core
			cout << ';' << getIPC(cstates1[i], cstates2[i]) <<
					';' << getCycles(cstates1[i], cstates2[i]) <<
					';' << getL3CacheMisses(cstates1[i], cstates2[i]) <<
					';' << getL2CacheMisses(cstates1[i], cstates2[i]) <<
					';' << getL3CacheHits(cstates1[i], cstates2[i]) <<
					';' << getL2CacheHits(cstates1[i], cstates2[i]) <<
					';' << getCyclesLostDueL3CacheMisses(cstates1[i], cstates2[i]) <<
					';' << getCyclesLostDueL2CacheMisses(cstates1[i], cstates2[i]) ;
		}
	}
	

}


void print_test(PCM * m,
	const std::vector<CoreCounterState> & cstates1,
	const std::vector<CoreCounterState> & cstates2,
	const std::vector<SocketCounterState> & sktstate1,
	const std::vector<SocketCounterState> & sktstate2,
	const SystemCounterState& sstate1,
	const SystemCounterState& sstate2,
	const int cpu_model
	)
{
	for (uint32 i = 0; i < 1/* m->getNumCores()*/; ++i)
	{
		cout << "\n" << (getCyclesLostDueTLBMisses(cstates1[i], cstates2[i])) <<
			";" << (getCycles(cstates1[i], cstates2[i])) <<
			";" << (getEvent0(cstates1[i], cstates2[i])) <<
			";" << (getEvent1(cstates1[i], cstates2[i])) <<
			";" << (getEvent2(cstates1[i], cstates2[i])) <<
			";" << (getEvent3(cstates1[i], cstates2[i])) ;
	}
	
}



int main(int argc, char * argv[])
{
#ifdef PCM_FORCE_SILENT
	null_stream nullStream1, nullStream2;
	std::cout.rdbuf(&nullStream1);
	std::cerr.rdbuf(&nullStream2);
#endif

#ifdef _MSC_VER
	// Increase the priority a bit to improve context switching delays on Windows
	SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_ABOVE_NORMAL);

	TCHAR driverPath[1040]; // length for current directory + "\\msr.sys"
	GetCurrentDirectory(1024, driverPath);
	wcscat_s(driverPath, 1040, L"\\msr.sys");

	SetConsoleCtrlHandler((PHANDLER_ROUTINE)cleanup, TRUE);
#else
	signal(SIGPIPE, cleanup);
	signal(SIGINT, cleanup);
	signal(SIGKILL, cleanup);
	signal(SIGTERM, cleanup);
#endif

	int delay = 25;

	char * sysCmd = NULL;
	bool show_core_output = true;
	bool show_socket_output = true;
	bool show_system_output = true;
	bool csv_output = true;
	bool disable_JKT_workaround = false; // as per http://software.intel.com/en-us/articles/performance-impact-when-sampling-certain-llc-events-on-snb-ep-with-vtune


	if (argc >= 2)
	{
		if (strcmp(argv[1], "--help") == 0 ||
			strcmp(argv[1], "-h") == 0 ||
			strcmp(argv[1], "/h") == 0)
		{
			print_help(argv[0]);
			return -1;
		}

		for (int l = 1; l < argc; l++)
		{
			if (argc >= 2)
			{
				if (strcmp(argv[l], "--help") == 0 ||
					strcmp(argv[l], "-h") == 0 ||
					strcmp(argv[l], "/h") == 0)
				{
					print_help(argv[l]);
					return -1;
				}
				else
					if (strcmp(argv[l], "--nocores") == 0 ||
						strcmp(argv[l], "-nc") == 0 ||
						strcmp(argv[l], "/nc") == 0)
					{
					show_core_output = false;
					}
					else

						if (strcmp(argv[l], "--nosockets") == 0 ||
							strcmp(argv[l], "-ns") == 0 ||
							strcmp(argv[l], "/ns") == 0)
						{
					show_socket_output = false;
						}

						else

							if (strcmp(argv[l], "--nosystem") == 0 ||
								strcmp(argv[l], "-nsys") == 0 ||
								strcmp(argv[l], "/nsys") == 0)
							{
					show_system_output = false;
							}

				if (strcmp(argv[l], "-csv") == 0 ||
					strcmp(argv[l], "/csv") == 0)
				{
					csv_output = true;
				}
				if (strcmp(argv[l], "--noJKTWA") == 0)
				{
					disable_JKT_workaround = true;
				}
			}
		}


#ifdef _MSC_VER
		if (strcmp(argv[1], "--uninstallDriver") == 0)
		{
			Driver tmpDrvObject;
			tmpDrvObject.uninstall();
			cout << "msr.sys driver has been uninstalled. You might need to reboot the system to make this effective." << endl;
			return 0;
		}
		if (strcmp(argv[1], "--installDriver") == 0)
		{
			Driver tmpDrvObject;
			if (!tmpDrvObject.start(driverPath))
			{
				cout << "Can not access CPU counters" << endl;
				cout << "You must have signed msr.sys driver in your current directory and have administrator rights to run this program" << endl;
				return -1;
			}
			return 0;
		}
#endif

		delay = atoi(argv[1]);
		if (delay <= 0)
		{
			sysCmd = argv[1];
		}
	}
	if (argc == 1)
	{
		print_help(argv[0]);
		return -1;
	}

#ifdef _MSC_VER
	// WARNING: This driver code (msr.sys) is only for testing purposes, not for production use
	Driver drv;
	// drv.stop();     // restart driver (usually not needed)
	if (!drv.start(driverPath))
	{
		cout << "Cannot access CPU counters" << endl;
		cout << "You must have signed msr.sys driver in your current directory and have administrator rights to run this program" << endl;
	}
#endif

	PCM * m = PCM::getInstance();
	if (disable_JKT_workaround) m->disableJKTWorkaround();
	/*switch (status)
	{
	case PCM::Success:
		break;
	case PCM::MSRAccessDenied:
		cerr << "Access to Intel(r) Performance Counter Monitor has denied (no MSR or PCI CFG space access)." << endl;
		return -1;
	case PCM::PMUBusy:
		cerr << "Access to Intel(r) Performance Counter Monitor has denied (Performance Monitoring Unit is occupied by other application). Try to stop the application that uses PMU." << endl;
		cerr << "Alternatively you can try to reset PMU configuration at your own risk. Try to reset? (y/n)" << endl;
		char yn;
		std::cin >> yn;
		if ('y' == yn)
		{
			m->resetPMU();
			cout << "PMU configuration has been reset. Try to rerun the program again." << endl;
		}
		return -1;
	default:
		cerr << "Access to Intel(r) Performance Counter Monitor has denied (Unknown error)." << endl;
		return -1;
	}*/

	cout << "\nDetected " << m->getCPUBrandString() << " \"Intel(r) microarchitecture codename " << m->getUArchCodename() << "\"" << endl;


	const int cpu_model = m->getCPUModel();

	uint64 TimeBeforeSleep = 0;
	uint64 TimeAfterSleep = 0;

	// TLB
	PCM::CustomCoreEventDescription descr[6];
	//descr[0].event_number = DTLB_MISSES_STLB_HIT_EVTNR;
	//descr[0].umask_value = DTLB_MISSES_STLB_HIT_UMASK;
	descr[0].event_number = DTLB_LOAD_MISSES_WALK_COMPLETE_EVTNR;
	descr[0].umask_value = DTLB_LOAD_MISSES_WALK_COMPLETE_UMASK;
	descr[1].event_number = DTLB_MISSES_ANY_EVTNR;
	descr[1].umask_value = DTLB_MISSES_ANY_UMASK;
	descr[2].event_number = MEM_LOAD_RETIRED_DTLB_MISS_EVTNR;
	descr[2].umask_value = MEM_LOAD_RETIRED_DTLB_MISS_UMASK;
	descr[3].event_number = MEM_STORE_RETIRED_DTLB_MISS_EVTNR;
	descr[3].umask_value = MEM_STORE_RETIRED_DTLB_MISS_UMASK;
	descr[4].event_number = L2_CACHE_LINE_INVALIDATION_EVTNR;
	descr[4].umask_value = L2_CACHE_LINE_INVALIDATION_UMASK;
	descr[5].event_number = L1_CACHE_LINE_INVALIDATION_EVTNR;
	descr[5].umask_value = L1_CACHE_LINE_INVALIDATION_UMASK;


	bool tlbMode = false;
	cout << "\n";

	freopen("output.csv", "w", stdout);
	cout << "BEGIN";
	while (1)
	{
	

		// Fresh containers
		std::vector<CoreCounterState> cstates1, cstates2;
		std::vector<SocketCounterState> sktstate1, sktstate2;
		SystemCounterState sstate1, sstate2;

		// Whether we should collect tlb or default
		PCM::ErrorCode status = tlbMode
			? m->program(PCM::ProgramMode::CUSTOM_CORE_EVENTS, &descr)
			: m->program();

		// Get the counters (t0)
		m->getAllCounterStates(sstate1, sktstate1, cstates1);
		TimeBeforeSleep = m->getTickCountRDTSCP(1000000);

		// Sleep (ms.)
		if (sysCmd) MySystem(sysCmd);
		else MySleepMs(delay);

		// Get the counter states (t1)
		m->getAllCounterStates(sstate2, sktstate2, cstates2);

		// Get the duration of our sampling
		TimeAfterSleep = m->getTickCountRDTSCP(1000000);
		auto duration = (TimeAfterSleep - TimeBeforeSleep);

		print_harvester(m, tlbMode, duration, cstates1, cstates2, sktstate1, sktstate2, sstate1, sstate2, cpu_model);
		//if (tlbMode)
		//	print_test(m, cstates1, cstates2, sktstate1, sktstate2, sstate1, sstate2, cpu_model);

		// Cleanup the PMU
		m->cleanup();

		// Switch the mode (round-roblin)
		tlbMode = tlbMode ? false : true;
		if (sysCmd)
		{
			// system() call removes PCM cleanup handler. need to do clean up explicitely
			PCM::getInstance()->cleanup();
			break;
		}
	}

	cout << std::flush;

	return 0;
}
