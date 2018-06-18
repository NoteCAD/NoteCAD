﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using g3;
using gs.info;

namespace gs
{

	public class MakerbotAssembler : BaseDepositionAssembler
    {
        public static BaseDepositionAssembler Factory(GCodeBuilder builder, SingleMaterialFFFSettings settings) {
            return new MakerbotAssembler(builder, settings);
        }


		public MakerbotSettings Settings;


		public MakerbotAssembler(GCodeBuilder useBuilder, SingleMaterialFFFSettings settings) : base(useBuilder, settings.Machine)
        {
            if (settings is MakerbotSettings == false)
                throw new Exception("MakerbotAssembler: incorrect settings type!");

            Settings = settings as MakerbotSettings;

            PositionBounds = new AxisAlignedBox2d(settings.Machine.BedSizeXMM, settings.Machine.BedSizeYMM);
            PositionBounds.Translate(-PositionBounds.Center);

            // [RMS] currently bed dimensions are hardcoded in header setup, and this trips bounds-checker.
            // So, disable this checking for now.
            EnableBoundsChecking = false;

            TravelGCode = 1;
		}


		public override void BeginRetract(Vector3d pos, double feedRate, double extrudeDist, string comment = null) {
            // [TODO] makerbot gcode disables fan here
            //		disable fan for every tiny travel? seems pointless...

            base.BeginRetract(pos, feedRate, extrudeDist, comment);

            // [RMS] makerbot does this...but does it do anything??
            base.BeginTravel();
            AppendMoveTo(pos, 3000, "Retract 2?");
            base.EndTravel();
		}

		public override void EndRetract(Vector3d pos, double feedRate, double extrudeDist = -9999, string comment = null) {
            base.EndRetract(pos, feedRate, extrudeDist, comment);

			// [TODO] re-enable fan here
		}




        public override void UpdateProgress(int i) {
			Builder.BeginMLine(73).AppendI("P",i);
		}


		public override void EnableFan() {
			Builder.BeginMLine(126).AppendI("T",0);
		}
		public override void DisableFan() {
			Builder.BeginMLine(127).AppendI("T",0);
		}

		public override void ShowMessage(string s) {
			// do nothing (not available)
		}




		public override void AppendHeader() {
			AppendHeader_Replicator2();
		}
		void AppendHeader_Replicator2() {

            base.AddStandardHeader(Settings);
			Builder.AddCommentLine("; Model: Makerbot " + Settings.ModelEnum.ToString());

            Vector2d BackRight = new Vector2d(152,75);
			Vector2d FrontLeft = new Vector2d(-141,-74);

			Vector2d PrimeFrontRight = new Vector2d(105.4, -74);
			double PrimeHeight = 0.270;



			Builder.BeginMLine(136, "(enable build)");

			// reset build percentage
			UpdateProgress(0);

			Builder.BeginGLine(162, "(home XY axes maximum)")
			       .AppendL("X").AppendL("Y").AppendI("F", 2000);

			Builder.BeginGLine(161, "(home Z axis minimum)")
			       .AppendL("Z").AppendI("F", 900);

			Builder.BeginGLine(92, "(set Z to -5)").
			       AppendI("X",0).AppendI("Y",0).AppendI("Z",-5).AppendI("A",0).AppendI("B",0);
			currentPos = new Vector3d(0, 0, -5);
			extruderA = 0;			

			Builder.BeginGLine(1, "(move Z to '0')")
			       .AppendI("Z", 0).AppendI("F", 900);
			currentPos.z = 0;		

			Builder.BeginGLine(161, "(home Z axis minimum)")
			       .AppendL("Z").AppendI("F", 100);

			Builder.BeginMLine(132, "(Recall stored home offsets for XYZAB axis)").
			       AppendL("X").AppendL("Y").AppendL("Z").AppendL("A").AppendL("B");

			Builder.BeginGLine(92).
			       AppendF("X",BackRight.x).AppendF("Y",BackRight.y).AppendI("Z",0).AppendI("A",0).AppendI("B",0);
			currentPos = new Vector3d(BackRight.x, BackRight.y, 0);
			extruderA = 0;

			Builder.BeginGLine(1, "(move to waiting position)").
			       AppendF("X",FrontLeft.x).AppendF("Y",FrontLeft.y).AppendI("Z",40).AppendI("F",3300);
			currentPos = new Vector3d(FrontLeft.x, FrontLeft.y, 40);

			Builder.BeginGLine(130, "(Lower stepper Vrefs while heating)").
			       AppendI("X",20).AppendI("Y",20).AppendI("A",20).AppendI("B",20);

			// set tool
			Builder.BeginMLine(135).AppendI("T",0);

			// set target temperature
			Builder.BeginMLine(104).AppendI("S",Settings.ExtruderTempC).AppendI("T",0);

			// wait to heat
			Builder.BeginMLine(133).AppendI("T",0);

			Builder.BeginGLine(130, "(Set Stepper motor Vref to defaults)").
			       AppendI("X",127).AppendI("Y",127).AppendI("A",127).AppendI("B",127);


            // thick line along front of bed, at start of print
            BeginTravel();
			AppendMoveTo(PrimeFrontRight.x, PrimeFrontRight.y, PrimeHeight, 9000, "(Extruder Prime Dry Move)");
            EndTravel();
			AppendMoveToE(FrontLeft.x, FrontLeft.y, PrimeHeight, 1800, 25, "(Extruder Prime Start)");

			Builder.BeginGLine(92,"(Reset after prime)").AppendI("A",0).AppendI("B",0);
			extruderA = 0;

			// move to z=0
			Builder.BeginGLine(1).AppendI("Z",0).AppendI("F",1000);
			currentPos.z = 0;

			// move to front-left corner
			AppendMoveToE(FrontLeft.x, FrontLeft.y, 0, 1000, 0);

			// reset E/A stepper 
			Builder.BeginGLine(92).AppendI("E",0);
			extruderA = 0;

			// should do this at higher level...
			//AppendMoveToA(FrontLeft.x, FrontLeft.y, 0, 1500, -1.3, "Retract");
			//AppendMoveTo(FrontLeft.x, FrontLeft.y, 0, 3000);		// what is this line for??
			//AppendMoveTo(FrontLeft.x, FrontLeft.y, LayerHeight, 1380, "Next Layer");

			in_retract = false;
			in_travel = false;

			UpdateProgress(0);
		}





		public override void AppendFooter() {
			AppendFooter_Replicator2();
		}
		void AppendFooter_Replicator2() {
			double MaxHeight = 155;

			Builder.AddCommentLine("End of print");

            // final retract
            if (InRetract == false) {
                BeginRetract(NozzlePosition, Settings.RetractSpeed,
                                        ExtruderA - Settings.RetractDistanceMM, "Final Retract");
            }

            //G1 X-9.119 Y10.721 Z0.200 F1500 A61.36007; Retract

            Builder.BeginMLine(127, "(Fan Off)").AppendI("T",0);
			Builder.BeginMLine(18, "(Turn off A and B Steppers)").AppendL("A").AppendL("B");

			// move bed to max height
			Builder.BeginGLine(1).AppendF("Z",MaxHeight).AppendI("F",900);

			// home steppers and turn off
			Builder.BeginGLine(162).AppendL("X").AppendL("Y").AppendI("F", 2000);
			Builder.BeginMLine(18,"(Turn off steppers)").AppendL("X").AppendL("Y").AppendL("Z");

			// set temp to 0
			Builder.BeginMLine(104).AppendI("S",0).AppendI("T",0);

			// set built-in status message
			Builder.BeginMLine(70, "(We <3 Making Things!)").AppendI("P",5);

			// skip song
			//Builder.BeginMLine(72).AppendI("P",1);

			UpdateProgress(100);

			Builder.BeginMLine(137,"(build end notification)");

			Builder.EndLine();		// need to force this
		}

	}


}