using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace gs.info
{
	public static class Monoprice
	{
        public const string UUID = "860432eb-dec6-4b20-8f97-3643a50daf1d";

        public enum Models {
            MP_Select_Mini_V2 = 0
        };

        public const string UUID_MP_Select_Mini_V2 = "4a498843-9080-4c97-aa82-b587f415ab1f";
    }


	public class MonopriceSettings : GenericRepRapSettings
    {
		public Monoprice.Models ModelEnum;

        public override AssemblerFactoryF AssemblerType() {
            return RepRapAssembler.Factory;
        }


		public MonopriceSettings(Monoprice.Models model) {
			ModelEnum = model;

            if (model == Monoprice.Models.MP_Select_Mini_V2)
                configure_MP_Select_Mini_V2();
        }

        public override T CloneAs<T>() {
            MonopriceSettings copy = new MonopriceSettings(this.ModelEnum);
            this.CopyFieldsTo(copy);
            return copy as T;
        }


        void configure_MP_Select_Mini_V2()
        {
            Machine.ManufacturerName = "Monoprice";
            Machine.ManufacturerUUID = Monoprice.UUID;
            Machine.ModelIdentifier = "MP Select Mini V2";
            Machine.ModelUUID = Monoprice.UUID_MP_Select_Mini_V2;
            Machine.Class = MachineClass.PlasticFFFPrinter;
            Machine.BedSizeXMM = 120;
            Machine.BedSizeYMM = 120;
            Machine.MaxHeightMM = 120;
            Machine.NozzleDiamMM = 0.4;
            Machine.FilamentDiamMM = 1.75;

            Machine.MaxExtruderTempC = 250;
            Machine.HasHeatedBed = true;
            Machine.MaxBedTempC = 60;

            Machine.MaxExtrudeSpeedMMM = 55 * 60;
            Machine.MaxTravelSpeedMMM = 150 * 60;
            Machine.MaxZTravelSpeedMMM = 100 * 60;
            Machine.MaxRetractSpeedMMM = 100 * 60;
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