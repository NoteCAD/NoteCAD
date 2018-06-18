using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace gs.info
{
	public static class RepRap
	{
        public const string UUID = "e95dcaaa-4315-412f-bd80-5049fb74f384";

        public enum Models {
            Unknown = 0
        };

        public const string UUID_Unknown = "bb097486-bb07-4a95-950f-1a1de992e782";
    }


	public class RepRapSettings : GenericRepRapSettings
    {
		public RepRap.Models ModelEnum;

        public override AssemblerFactoryF AssemblerType() {
            return RepRapAssembler.Factory;
        }


		public RepRapSettings(RepRap.Models model) {
			ModelEnum = model;

            if (model == RepRap.Models.Unknown)
                configure_unknown();
        }

        public override T CloneAs<T>() {
            RepRapSettings copy = new RepRapSettings(this.ModelEnum);
            this.CopyFieldsTo(copy);
            return copy as T;
        }


        void configure_unknown()
        {
            Machine.ManufacturerName = "RepRap";
            Machine.ManufacturerUUID = RepRap.UUID;
            Machine.ModelIdentifier = "Unknown";
            Machine.ModelUUID = RepRap.UUID_Unknown;
            Machine.Class = MachineClass.PlasticFFFPrinter;
            Machine.BedSizeXMM = 80;
            Machine.BedSizeYMM = 80;
            Machine.MaxHeightMM = 55;
            Machine.NozzleDiamMM = 0.4;
            Machine.FilamentDiamMM = 1.75;

            Machine.MaxExtruderTempC = 230;
            Machine.HasHeatedBed = false;
            Machine.MaxBedTempC = 60;

            Machine.MaxExtrudeSpeedMMM = 50 * 60;
            Machine.MaxTravelSpeedMMM = 150 * 60;
            Machine.MaxZTravelSpeedMMM = 100 * 60;
            Machine.MaxRetractSpeedMMM = 40 * 60;
            Machine.MinLayerHeightMM = 0.1;
            Machine.MaxLayerHeightMM = 0.3;

            LayerHeightMM = 0.2;

            ExtruderTempC = 200;
            HeatedBedTempC = 0;

            SolidFillNozzleDiamStepX = 1.0;
            RetractDistanceMM = 4.5;

            RetractSpeed = Machine.MaxRetractSpeedMMM;
            ZTravelSpeed = Machine.MaxZTravelSpeedMMM;
            RapidTravelSpeed = Machine.MaxTravelSpeedMMM;
            CarefulExtrudeSpeed = 20 * 60;
            RapidExtrudeSpeed = Machine.MaxExtrudeSpeedMMM;
            OuterPerimeterSpeedX = 0.5;
        }


    }

}