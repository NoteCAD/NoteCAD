using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace gs.info
{
	public static class Makerbot
	{
        public const string UUID = "77b7ed08-dcc8-4c2e-a189-18aa549bf94b";

        public enum Models {
            Unknown,
			Replicator2
		};

        public const string UUID_Unknown = "625aa5dc-8e9d-4240-86ff-8bc369cd5124";
        public const string UUID_Replicator2 = "a1c13f61-1ae6-4b1a-9c8c-18b2170e82b1";
    }


	public class MakerbotSettings : SingleMaterialFFFSettings
	{
        public Makerbot.Models ModelEnum;

        public override AssemblerFactoryF AssemblerType() {
            return MakerbotAssembler.Factory;
        }

        public MakerbotSettings(Makerbot.Models model = Makerbot.Models.Replicator2) {
			ModelEnum = model;

            if (model == Makerbot.Models.Replicator2)
                configure_Replicator_2();
            else
                configure_unknown();

        }


        public override T CloneAs<T>() {
            MakerbotSettings copy = new MakerbotSettings(this.ModelEnum);
            this.CopyFieldsTo(copy);
            return copy as T;
        }


        void configure_Replicator_2()
        {
            Machine.ManufacturerName = "Makerbot";
            Machine.ManufacturerUUID = Makerbot.UUID;
            Machine.ModelIdentifier = "Replicator 2";
            Machine.ModelUUID = Makerbot.UUID_Replicator2;
            Machine.Class = MachineClass.PlasticFFFPrinter;
            Machine.BedSizeXMM = 285;
            Machine.BedSizeYMM = 153;
            Machine.MaxHeightMM = 155;
            Machine.NozzleDiamMM = 0.4;
            Machine.FilamentDiamMM = 1.75;

            Machine.MaxExtruderTempC = 230;
            Machine.HasHeatedBed = false;
            Machine.MaxBedTempC = 0;

            Machine.MaxExtrudeSpeedMMM = 90 * 60;
            Machine.MaxTravelSpeedMMM = 150 * 60;
            Machine.MaxZTravelSpeedMMM = 23 * 60;
            Machine.MaxRetractSpeedMMM = 25 * 60;
            Machine.MinLayerHeightMM = 0.1;
            Machine.MaxLayerHeightMM = 0.3;


            LayerHeightMM = 0.2;

            ExtruderTempC = 230;
            HeatedBedTempC = 0;

            SolidFillNozzleDiamStepX = 1.0;
            RetractDistanceMM = 1.3;

            RetractSpeed = Machine.MaxRetractSpeedMMM;
            ZTravelSpeed = Machine.MaxZTravelSpeedMMM;
            RapidTravelSpeed = Machine.MaxTravelSpeedMMM;
            CarefulExtrudeSpeed = 30 * 60;
            RapidExtrudeSpeed = Machine.MaxExtrudeSpeedMMM;
            OuterPerimeterSpeedX = 0.5;
        }


        void configure_unknown()
        {
            Machine.ManufacturerName = "Makerbot";
            Machine.ManufacturerUUID = Makerbot.UUID;
            Machine.ModelIdentifier = "(Unknown)";
            Machine.ModelUUID = Makerbot.UUID_Unknown;
            Machine.Class = MachineClass.PlasticFFFPrinter;

            Machine.BedSizeXMM = 100;
            Machine.BedSizeYMM = 100;
            Machine.MaxHeightMM = 130;
            Machine.NozzleDiamMM = 0.4;
            Machine.FilamentDiamMM = 1.75;

            Machine.MaxExtruderTempC = 230;
            Machine.HasHeatedBed = false;
            Machine.MaxBedTempC = 0;
            Machine.MaxExtrudeSpeedMMM = 90 * 60;
            Machine.MaxTravelSpeedMMM = 150 * 60;
            Machine.MaxZTravelSpeedMMM = 23 * 60;
            Machine.MaxRetractSpeedMMM = 25 * 60;
            Machine.MinLayerHeightMM = 0.1;
            Machine.MaxLayerHeightMM = 0.3;


            LayerHeightMM = 0.2;

            ExtruderTempC = 230;
            HeatedBedTempC = 0;

            SolidFillNozzleDiamStepX = 1.0;
            RetractDistanceMM = 1.3;

            RetractSpeed = Machine.MaxRetractSpeedMMM;
            ZTravelSpeed = Machine.MaxZTravelSpeedMMM;
            RapidTravelSpeed = Machine.MaxTravelSpeedMMM;
            CarefulExtrudeSpeed = 30 * 60;
            RapidExtrudeSpeed = Machine.MaxExtrudeSpeedMMM;
            OuterPerimeterSpeedX = 0.5;
        }

    }

}