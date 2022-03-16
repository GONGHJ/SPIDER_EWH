using System.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyApplication
{
    class EWH
    {    
        // outputs from EWH model
        private double P_EWH;   // EWH electric power
        private double TEM_TANK;   // Water temperature in the tank
        private int EWH_ON;     // EWH opearation flag


        // inputs from user. updates each time step from two csv files.
        public double HWD;     // hot water draw, unit: gallon/minute
        public double TEM_AMB;  // ambient air temperature, unit: F
        public double TEM_ICWT; // cold water temperature, unit: F
        public string Event_type; // Event types

        // water heater parameters: read from csv file
        public double VOL_W;   // Water tank Volume
        public double RP_EWH;   // Rated power of EWH: kW
        public double EFF_HE;   // Efficiency of heating element
        public double EQUIC; // Equivalent capacitance, calculated based on the previous three values

        // physical constants
        public const double DEN_W = 993;   // Density of water
        public const double SHC_W = 4179;   // Specific heat capacity of water
        public const double EQUIR = 1500;   // Equivalent resistance: resistive EWH
        public const double JouletoKwh = 2.77778e-7;
        public const double Gallontom3 = 0.00378541;
       
        // internal variable
        private double EnergyTakeCap_NO_max;         // energy take capacity for normal operation: Wh, change with flow rate
        private double EnergyTakeCap_NO_min = 0;         // energy take capacity for normal operation: Wh
        private double EnergyTakeCap_Shed_max = 2250;       // energy take capacity for shed event: Wh
        private double EnergyTakeCap_Shed_min = 1800;       // energy take capacity for shed event: Wh
        private double EnergyTakeCap_Load_max = 300;       // energy take capacity for shed event: Wh
        private double EnergyTakeCap_Load_min = 0;       // energy take capacity for shed event: Wh

        // simulation settings
        private const double Sim_resoluation = 1.0 /60; // 1 min example

        // The EWH model
        public (int, double, double) EWH_TempCal(double TEM_TANK_Reco_F, int EWH_ON_Reco, double EnergyTaken_Reco)
        {
            switch (Event_type)                               // determine ON/OFF status according to different mode
            {
                case "Normal_Operation":
                    if (HWD > 1)            // high flow rate
                    {
                        EnergyTakeCap_NO_max = 300;
                    }
                    else if (HWD > 0.3)     // middle flow rate
                    {
                        EnergyTakeCap_NO_max = 600;
                    }
                    else                    // low flow rate
                    {
                        EnergyTakeCap_NO_max = 900;
                    }
                    if (EWH_ON_Reco == 0 && EnergyTaken_Reco > EnergyTakeCap_NO_max)      // was off but need to be turn on
                    {
                        EWH_ON = 1;
                    }
                    else if (EWH_ON_Reco == 1 && EnergyTaken_Reco < EnergyTakeCap_NO_min)                   // was on but need to be turn off
                    {
                        EWH_ON = 0;
                    }
                    else
                    {
                        EWH_ON = EWH_ON_Reco;
                    }
                    break;
                case "Shed":
                    if (EWH_ON_Reco == 0 && EnergyTaken_Reco > EnergyTakeCap_Shed_max)      // was off but need to be turned on
                    {
                        EWH_ON = 1;
                    }
                    else if (EWH_ON_Reco == 1 && EnergyTaken_Reco < EnergyTakeCap_Shed_min)                   // was on but need to be turned off
                    {
                        EWH_ON = 0;
                    }
                    else
                    {
                        EWH_ON = EWH_ON_Reco;
                    }
                    break;
                case "LoadUp":
                    if (EWH_ON_Reco == 0 && EnergyTaken_Reco > EnergyTakeCap_Load_max)      // was off but need to be turned on
                    {
                        EWH_ON = 1;
                    }
                    else if (EWH_ON_Reco == 1 && EnergyTaken_Reco < EnergyTakeCap_Load_min)                   // was on but need to be turned off
                    {
                        EWH_ON = 0;
                    }
                    else
                    {
                        EWH_ON = EWH_ON_Reco;
                    }
                    break;
            }
            // input unit converter
            P_EWH = EWH_ON * RP_EWH;            // P = ON/PFF * rated power
            double TEM_TANK_Reco = (TEM_TANK_Reco_F - 32) * 5 / 9;    // F to C
            HWD = HWD * EWH.Gallontom3;     // water flow: gallon to m3
            TEM_AMB = (TEM_AMB - 32) * 5 / 9;       //F to C
            TEM_ICWT = (TEM_ICWT - 32) * 5 / 9;     // F to C
            // differential equation
            TEM_TANK = TEM_TANK_Reco + Sim_resoluation / EQUIC * (EWH_ON_Reco * RP_EWH * EFF_HE - 1 / EQUIR * (TEM_TANK_Reco - TEM_AMB) 
                - SHC_W * DEN_W * HWD * (TEM_TANK_Reco - TEM_ICWT) * JouletoKwh / Sim_resoluation);      // calculation the temperature in the tank
            // output unit converter
            TEM_TANK = TEM_TANK * 9 / 5 + 32;           // C to F
            HWD = HWD / EWH.Gallontom3;     // water flow: m3 to gallon 
            TEM_AMB = TEM_AMB  * 9 / 5 + 32;           // C to F
            TEM_ICWT = TEM_ICWT * 9 / 5 + 32;           // C to F
            //
            return (EWH_ON, TEM_TANK, P_EWH);
        }
    }
}



    
