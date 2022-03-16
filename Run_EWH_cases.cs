using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MyApplication
{
    class EWHexample
    {
        static void Main()
        {
            //*****************user defines the example EWH type, and the event type.
            string EWHtype = "EWH_1.csv";   // pick EWH_1 as example
            //user defined case: pick one out of the three, comment out the other two
            //string Example_case = "Normal_Operation_Examplecase";
           // string Example_case = "Shed_Examplecase";
            string Example_case = "Loadup_Examplecase";

            EWH Resistive_EWH = new EWH();                  // load new class

            // assign EWH parameters from the CSV file
            (double, double, double, double) EWHtype_defination(string EWHtype)  // read water flow: gallon/mimute, ambien temperature [F], inlet cold water temperature [F]
            {
                string[] read;
                char[] seperators = { ',' }; 
                StreamReader sr = new StreamReader("../../../EWHtypes/" + EWHtype);

                string data = sr.ReadLine();
                data = sr.ReadLine();
                read = data.Split(seperators, StringSplitOptions.None);
                double VOL_W = double.Parse(read[0]);   // Water tank Volume
                double RP_EWH = double.Parse(read[1]);   // Rated power of EWH: kW
                double EFF_HE = double.Parse(read[2]);   // Efficiency of heating element
                double EQUIC = VOL_W * EWH.Gallontom3 * EWH.DEN_W * EWH.SHC_W * EWH.JouletoKwh; // Equivalent capacitance
                return (VOL_W, RP_EWH, EFF_HE, EQUIC);
            }
            // read inputs from csv file, updates at each time step
            (string, double, double, double) Read_inputfile(int timestep, string HWD_filename)  // read water flow: gallon/mimute, ambien temperature [F], inlet cold water temperature [F]
            {
                string[] read;
                char[] seperators = { ',' }; 
                StreamReader sr = new StreamReader("../../../Inputs/" + HWD_filename);
                string data = sr.ReadLine();
                for (int i = 0; i < timestep; i++)
                {
                    sr.ReadLine();
                }
                data = sr.ReadLine();
                read = data.Split(seperators, StringSplitOptions.None);
                string Timestap = read[0];                 // read the timestap
                double HWD = double.Parse(read[1]);                 // hot water draw: gallom/minute
                double Temp_AMB = double.Parse(read[2]);            // ambien temperature: F
                double Temp_ICWT = double.Parse(read[3]);            // inlet cold water temperature: F
                return (Timestap, HWD, Temp_AMB, Temp_ICWT);   // gallon/minute 
            }
            // read Event type from csv file, updates at each time step
            string Read_event(int timestep, string HWD_Event_filename)  
            {
                string[] read;
                char[] seperators = { ',' }; ;
                StreamReader sr = new StreamReader("../../../Inputs/" + HWD_Event_filename);
                string data = sr.ReadLine();
                for (int i = 0; i < timestep; i++)
                {
                    sr.ReadLine();
                }
                data = sr.ReadLine();
                read = data.Split(seperators, StringSplitOptions.None);
                string Event = read[1];                 // read the timestap
                return Event;   // gallon/minute 
            }

            //initialize the outputs for recording 
            List<string> Timestap = new List<string> { "Initialize" };    // hot water draw, unit: gallom per minute
            List<double> HWD = new List<double> { 0 };    // hot water draw, unit: gallom per minute
            List<double> Temp_AMB = new List<double> { 0 };    // ambiente temperature, unit: F
            List<double> Temp_ICWT = new List<double> { 0 };    // inlet cold water temperature, unit :F
            List<int> EWH_ON_History = new List<int> { 0 };     // ON/OFF record. ON: 1; OFF: 0
            List<double> P_EWH_History = new List<double> { 0 };    // EWH power record: unit: W
            List<double> TEM_TANK_History = new List<double> { 125 }; // Tank temperature record, Unit: F. Initial value is defined for different cases
            List<double> EnergyTake_History = new List<double> { 0 }; //EnergyTake record: unit: Wh. Initial value is defined for different cases
            List<double> EnergyStorage_History = new List<double> { 0 }; //EnergyTake record: unit: Wh. Initial value is defined for different cases
            List<string> Event_History = new List<string> { "NormalOperation" };    // hot water draw, unit: gallom per minute
            // assing the EWH parameteres from the csv file
            (Resistive_EWH.VOL_W, Resistive_EWH.RP_EWH, Resistive_EWH.EFF_HE, Resistive_EWH.EQUIC) = EWHtype_defination(EWHtype);

            // initialize the the total simulation steps, input file names for different event examples. The value is default for "normal operation"
            string HWD_filename = "NormalOP_Input.csv";             // Input file for hot water draw: default as normal operation 
            string HWD_Event_filename = "NormalOP_Event.csv";   // Input file for event: default as normal operation
            int Simsteps_total = 45 ;            // total simulation steps, default value for "Normal operation"
             // Initialize the hot water draw and DR event occurance time
            switch (Example_case)
            {
                case "Normal_Operation_Examplecase":
                    break;
                case "Shed_Examplecase":
                    HWD_filename = "Shed_Input.csv";
                    HWD_Event_filename = "Shed_Event.csv";
                    Simsteps_total = 107;
                    break;
                case "Loadup_Examplecase":
                    HWD_filename = "Loadup_Input.csv";
                    HWD_Event_filename = "Loadup_Event.csv";
                    Simsteps_total = 33;
                    TEM_TANK_History[0] = 123;
                    EnergyTake_History[0] = 300;
                    break;
            }

            // run the EWH model
            for (int i = 0; i < Simsteps_total; i++)
            {               
                string Timestap_onestep;
                (Timestap_onestep, Resistive_EWH.HWD, Resistive_EWH.TEM_AMB, Resistive_EWH.TEM_ICWT) = Read_inputfile(i, HWD_filename);   // read input files: timesstap, hot water draw, ambien temperature, cold water tempearture
                Resistive_EWH.Event_type = Read_event(i, HWD_Event_filename);       // read event type
                (int EWH_ON_temp, double TEM_TANK_temp, double P_EWH_temp) = Resistive_EWH.EWH_TempCal(TEM_TANK_History[i], EWH_ON_History[i], EnergyTake_History[i]);  // iterate according to the differential equation
                // calculate the taken away energy by hot water
                double EnergyTake_oneStep = (TEM_TANK_History[i] - TEM_TANK_temp) *5/9* EWH.SHC_W * EWH.DEN_W * Resistive_EWH.VOL_W * EWH.Gallontom3 * EWH.JouletoKwh * 1000;   //calculate the taken away energy by hot water at one time step: Wh                                                                                                                           
                double EnergyTake_agg = EnergyTake_History[i] + EnergyTake_oneStep;   //calculate the cumulative taken away energy by hot water: kWh
                double EnergyStorage= (TEM_TANK_temp - Resistive_EWH.TEM_ICWT) *5/9 * EWH.SHC_W * EWH.DEN_W * Resistive_EWH.VOL_W * EWH.Gallontom3 * EWH.JouletoKwh * 1000;  // calculate the energy stored: Wh.
                //recording the outputs for the csv file
                Timestap.Add(Timestap_onestep);                 // record the timestap
                HWD.Add(Resistive_EWH.HWD);    // unit: gallom per minute
                Temp_AMB.Add(Resistive_EWH.TEM_AMB);  // unit: F
                Temp_ICWT.Add(Resistive_EWH.TEM_ICWT);    // inlet cold water temperature, unit :F
                EWH_ON_History.Add(EWH_ON_temp);        // Temporary EWH ON/OFF status
                TEM_TANK_History.Add(TEM_TANK_temp);   // unit: F
                P_EWH_History.Add(P_EWH_temp * 1000);   //  unit: W
                EnergyTake_History.Add(EnergyTake_agg);         //save as Wh
                EnergyStorage_History.Add(EnergyStorage);                 //save as Wh
                Event_History.Add(Resistive_EWH.Event_type);                     // save operation Event
            }
            // save data as "Results.csv"      
            var csv = new StringBuilder();
            var newLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}","TimeStap", "How water draw [GPM]","Ambient Temp [F]","Inlet cold water Temp [F]","Tank Temp [F]", "Power [W]", "EWH_ON_OFF", "Energy take [Wh]","Energy storage [Wh]","Event type");
            csv.AppendLine(newLine);
            for (int i = 1; i < P_EWH_History.Count; i++)
            {
                csv.AppendLine(string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}", Timestap[i], HWD[i], Temp_AMB[i], Temp_ICWT[i], TEM_TANK_History[i], P_EWH_History[i], EWH_ON_History[i], EnergyTake_History[i], EnergyStorage_History[i], Event_History[i]));
            }
            File.WriteAllText("../../../Outputs/" + Example_case +"_Results.csv", csv.ToString());
        }
    }
}

