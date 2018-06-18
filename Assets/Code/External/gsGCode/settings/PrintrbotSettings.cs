using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;

namespace gs.info
{
	public static class Printrbot
	{
		public const string UUID = "10dd0db9-df41-4b3e-8b73-d345c408b5ff";

        public enum Models {
            Plus = 0
        };

		public const string UUID_Plus = "1c868178-5aa2-47c3-9a8c-6f86ef1d1ff6";
    }


	public class PrintrbotSettings : GenericRepRapSettings
    {
		public Printrbot.Models ModelEnum;

        public bool EnableAutoBedLevel;     // should we auto-bed-level?


        public override AssemblerFactoryF AssemblerType() {
			return MakePrintrbotAssembler;
        }


		public PrintrbotSettings(Printrbot.Models model) {
			ModelEnum = model;

			if (model == Printrbot.Models.Plus)
                configure_Plus();
        }

        public override T CloneAs<T>() {
			PrintrbotSettings copy = new PrintrbotSettings(this.ModelEnum);
            this.CopyFieldsTo(copy);
            return copy as T;
        }

        protected override void CopyFieldsTo(SingleMaterialFFFSettings to)
        {
            base.CopyFieldsTo(to);
            if (to is PrintrbotSettings)
                (to as PrintrbotSettings).EnableAutoBedLevel = this.EnableAutoBedLevel;
        }


        void configure_Plus()
        {
            Machine.ManufacturerName = "Printrbot";
			Machine.ManufacturerUUID = Printrbot.UUID;
            Machine.ModelIdentifier = "Plus";
			Machine.ModelUUID = Printrbot.UUID_Plus;
            Machine.Class = MachineClass.PlasticFFFPrinter;
            Machine.BedSizeXMM = 250;
            Machine.BedSizeYMM = 250;
            Machine.MaxHeightMM = 250;
            Machine.NozzleDiamMM = 0.4;
            Machine.FilamentDiamMM = 1.75;

            Machine.MaxExtruderTempC = 250;
            Machine.HasHeatedBed = true;
            Machine.MaxBedTempC = 80;

            Machine.MaxExtrudeSpeedMMM = 80 * 60;
            Machine.MaxTravelSpeedMMM = 120 * 60;
            Machine.MaxZTravelSpeedMMM = 100 * 60;
            Machine.MaxRetractSpeedMMM = 45 * 60;
            Machine.MinLayerHeightMM = 0.05;
            Machine.MaxLayerHeightMM = 0.3;


            LayerHeightMM = 0.2;

            ExtruderTempC = 200;
            HeatedBedTempC = 0;

            SolidFillNozzleDiamStepX = 1.0;
            RetractDistanceMM = 0.7;

            RetractSpeed = Machine.MaxRetractSpeedMMM;
            ZTravelSpeed = Machine.MaxZTravelSpeedMMM;
            RapidTravelSpeed = Machine.MaxTravelSpeedMMM;
            CarefulExtrudeSpeed = 20 * 60;
            RapidExtrudeSpeed = Machine.MaxExtrudeSpeedMMM;
            OuterPerimeterSpeedX = 0.5;


            // specific to printrbot
            EnableAutoBedLevel = true;
        }




		public BaseDepositionAssembler MakePrintrbotAssembler(
			GCodeBuilder builder, SingleMaterialFFFSettings settings)
		{
			var asm = new RepRapAssembler(builder, settings);
			asm.HeaderCustomizerF = HeaderCustomF;
			return asm;
		}

		protected void HeaderCustomF(RepRapAssembler.HeaderState state, GCodeBuilder Builder)
		{
			if (state == RepRapAssembler.HeaderState.BeforePrime) {
                if ( EnableAutoBedLevel )
				    Builder.BeginGLine(29, "auto-level bed");
			}
		}

    }

}