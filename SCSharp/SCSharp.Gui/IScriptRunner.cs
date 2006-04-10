
/* opcode descriptions and info here 
http://www.campaigncreations.org/starcraft/bible/chap4_ice_opcodes.shtml
http://www.stormcoast-fortress.net/cntt/tutorials/camsys/tilesetdependent/?PHPSESSID=7365d884cf33fc614c0b96d966872177

*/


using SdlDotNet;
using System.Drawing;
using System.Drawing.Imaging;

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace SCSharp {

	public class IScriptRunner {
		static Random rng = new Random (Environment.TickCount);

		/* opcodes */
		const byte PlayFrame = 0x00;
		const byte PlayTilesetFrame = 0x01;
		/* 0x02 = unknown */
		const byte ShiftGraphicVert = 0x03;
		/* 0x04 = unknown */
		const byte Wait = 0x05;
		const byte Wait2Rand = 0x06;
		const byte Goto = 0x07;
		const byte PlaceActiveOverlay = 0x08;
		const byte PlaceActiveUnderlay = 0x09;
		/* 0x0a = unknown */
		const byte SwitchUnderlay = 0x0b; /* unknown according to sceb */
		/* 0x0c = unknown */
		const byte PlaceOverlay = 0x0d;
		/* 0x0e = unknown */
		const byte PlaceIndependentOverlay = 0x0f;
		const byte PlaceIndependentOverlayOnTop = 0x10;
		const byte PlaceIndependentUnderlay = 0x11;
		/* 0x12 = unknown */
		const byte DisplayOverlayWithLO = 0x13;
		/* 0x14 = unknown */
		const byte DisplayIndependentOverlayWithLO = 0x15;
		const byte EndAnimation = 0x16;
		/* 0x17 = unknown */
		const byte PlaySound = 0x18;
		const byte PlayRandomSound = 0x19;
		const byte PlayRandomSoundRange = 0x1a;
		const byte DoDamage = 0x1b;
		const byte AttackWithWeaponAndPlaySound = 0x1c;
		const byte FollowFrameChange = 0x1d;
		const byte RandomizerValueGoto = 0x1e;
		const byte TurnCCW = 0x1f;
		const byte TurnCW = 0x20;
		const byte Turn1CW = 0x21;
		const byte TurnRandom = 0x22;
		/* 0x23 = unknown */
		const byte Attack = 0x25;
		const byte AttackWithAppropriateWeapon = 0x26;
		const byte CastSpell = 0x27;
		const byte UseWeapon = 0x28;
		const byte MoveForward = 0x29;
		const byte AttackLoopMarker = 0x2a;
		/* 0x2b = unknown */
		/* 0x2c = unknown */
		/* 0x2d = unknown */
		const byte BeginPlayerLockout = 0x2e;
		const byte EndPlayerLockout = 0x2f;
		const byte IgnoreOtherOpcodes = 0x30;
		const byte AttackWithDirectionalProjectile = 0x31;
		const byte Hide = 0x32;
		const byte Unhide = 0x33;
		const byte PlaySpecificFrame = 0x34;
		/* 0x35 = unknown */
		/* 0x36 = unknown */
		/* 0x37 = unknown */
		/* 0x38 = unknown */
		const byte IfPickedUp = 0x39;
		const byte IfTargetInRangeGoto = 0x3a;
		const byte IfTargetInArcGoto = 0x3b;
		/* 0x3c = unknown */
		/* 0x3d = unknown */
		/* 0x3e = unknown */
		/* 0x3f = unknown */
		/* 0x40 = unknown */
		/* 0x41 = unknown */

		byte[] buf;
		ushort script_start; /* the pc of the script that started us off */
		ushort pc;
		Grp grp;
		ushort script_entry_offset;

		int facing = 0;

		void Trace (string fmt, params object[] args)
		{
			Console.Write (fmt, args);
		}

		void TraceLine (string fmt, params object[] args)
		{
			Console.WriteLine (fmt, args);
		}

		public IScriptRunner (Grp grp, ushort script_entry_offset)
		{
			this.grp = grp;
			this.buf = GlobalResources.Instance.IScriptBin.Contents;
			this.script_entry_offset = script_entry_offset;

			/* make sure the offset points to "SCEP" */
		}

		public void RunScript (ushort script_start)
		{
			this.script_start = script_start;
			pc = script_start;
		}

		public void RunScriptType (int script_type)
		{
			TraceLine ("running script type = {0}", script_type);

			int offset_to_script_type = (4 /* "SCEP" */ + 1 /* the script entry "type" */ + 3 /* the spacers */ +
						     script_type * 2);

			script_start = Util.ReadWord (buf, script_entry_offset + offset_to_script_type);
			pc = script_start;
			TraceLine ("pc = {0}", pc);
		}

		ushort ReadWord (ref ushort pc)
		{
			ushort retval = Util.ReadWord (buf, pc);
			pc += 2;
			return retval;
		}

		byte ReadByte (ref ushort pc)
		{
			byte retval = buf[pc];
			pc ++;
			return retval;
		}

		Painter painter;
		Surface sprite_surface;
		
		void PaintSprite (Surface surf, DateTime now)
		{
			if (sprite_surface != null) {
				Console.WriteLine ("blitting");
				surf.Blit (sprite_surface /* XXX position */);
			}
		}

		public void AddToPainter (Painter painter)
		{
			this.painter = painter;
			painter.Add (Layer.Unit, PaintSprite);
		}

		public void RemoveFromPainter (Painter painter)
		{
			painter.Add (Layer.Unit, PaintSprite);
			this.painter = null;
		}

		void DoPlayFrame (Surface painter_surface, int frame_num)
		{
			if (sprite_surface != null)
				sprite_surface.Dispose();

			// XXX
			sprite_surface = GuiUtil.CreateSurfaceFromBitmap (grp.GetFrame (frame_num),
									  grp.Width, grp.Height,
									  null /*Palette.default_palette*/,
									  false);
		}

		int waiting;

		public bool Tick (Surface surf, DateTime now)
		{
			Console.WriteLine ("in IScriptRunner.Tick");
			ushort warg1;
			ushort warg2;
			//			ushort warg3;
			byte barg1;
			byte barg2;
			//			byte barg3;

			if (pc == 0)
				return true;

			if (waiting > 0) {
				waiting--;
				return true;
			}
			
			Trace ("{0}: ", pc);
			switch (buf[pc++]) {
			case PlayFrame:
				warg1 = ReadWord (ref pc);
				TraceLine ("PlayFrame: {0}", warg1);
				DoPlayFrame (surf, warg1 + facing % 16);
				break;
			case PlayTilesetFrame:
				warg1 = ReadWord (ref pc);
				TraceLine ("PlayTilesetFrame: {0}", warg1);
				break;
			case ShiftGraphicVert:
				barg1 = ReadByte (ref pc);
				TraceLine ("ShiftGraphicVert: {0}", barg1);
				break;
			case Wait:
				barg1 = ReadByte (ref pc);
				TraceLine ("Wait: {0}", barg1);
				waiting = barg1;
				break;
			case Wait2Rand:
				barg1 = ReadByte (ref pc);
				barg2 = ReadByte (ref pc);
				TraceLine ("Wait2: {0} {1}", barg1, barg2);
				waiting = rng.Next(255) > 127 ? barg1 : barg2;
				break;
			case Goto:
				warg1 = ReadWord (ref pc);
				TraceLine ("Goto: {0}", warg1);
				pc = warg1;
				break;
			case PlaceActiveOverlay:
				warg1 = ReadWord (ref pc);
				warg2 = ReadWord (ref pc);
				TraceLine ("PlaceActiveOverlay: {0} {1}", warg1, warg2);
				break;
			case PlaceActiveUnderlay:
				warg1 = ReadWord (ref pc);
				warg2 = ReadWord (ref pc);
				TraceLine ("PlaceActiveUnderlay: {0} {1}", warg1, warg2);
				break;
			case MoveForward:
				barg1 = ReadByte (ref pc);
				TraceLine ("Move forward %1 units: {0}", barg1);
				break;
			case RandomizerValueGoto:
				barg1 = ReadByte (ref pc);
				warg1 = ReadWord (ref pc);
				TraceLine ("Randomized (with test value) goto: {0} {1}", barg1, warg1);
				int rand = rng.Next(255);
				if (rand > barg1) {
					TraceLine ("+ choosing goto branch");
					pc = warg1;
				}
				break;
			case TurnRandom:
				TraceLine ("Turn graphic number of frames in random direction (CCW or CW)");
				if (rng.Next(255) > 127)
					goto case TurnCCW;
				else
					goto case TurnCW;
			case TurnCCW:
				barg1 = ReadByte (ref pc);
				TraceLine ("Turn graphic number of frames CCW: {0}", barg1);
				if (facing - barg1 < 0)
					facing = 15 - barg1;
				else
					facing -= barg1;
				break;
			case TurnCW:
				barg1 = ReadByte (ref pc);
				TraceLine ("Turn graphic number of frames CW: {0}", barg1);
				if (facing + barg1 > 15)
					facing = facing + barg1 - 15;
				else
					facing += barg1;
				break;
			case Turn1CW:
				TraceLine ("Turn graphic 1 frame clockwise");
				break;
			case PlaySound:
				warg1 = ReadWord (ref pc);
				TraceLine ("Play sound: {0} ({1})", warg1 - 1, GlobalResources.Instance.SfxDataTbl[GlobalResources.Instance.SfxDataDat.GetFileIndex ((uint)(warg1 - 1))]);
				break;
			case PlayRandomSoundRange:
				warg1 = ReadWord (ref pc);
				warg2 = ReadWord (ref pc);
				TraceLine ("Play random sound in range: {0}-{1}", warg1, warg2);
				break;
			case PlaySpecificFrame:
				barg1 = ReadByte (ref pc);
				TraceLine ("PlaySpecificFrame: {0}", barg1);
				DoPlayFrame (surf, barg1);
				break;
			case PlaceIndependentUnderlay:
				warg1 = ReadWord (ref pc);
				barg1 = ReadByte (ref pc);
				barg2 = ReadByte (ref pc);
				TraceLine ("PlaceIndependentUnderlay: {0} ({1},{2})", warg1, barg1, barg2);
				Sprite s = SpriteManager.CreateSprite (warg1);
				s.RunAnimation (0);
				break;
			case EndAnimation:
				return false;
			case SwitchUnderlay:
			case PlaceOverlay:
			case PlaceIndependentOverlay:
			case PlaceIndependentOverlayOnTop:
			case DisplayOverlayWithLO:
			case DisplayIndependentOverlayWithLO:
			case PlayRandomSound:
			case DoDamage:
			case AttackWithWeaponAndPlaySound:
			case FollowFrameChange:
			case Attack:
			case AttackWithAppropriateWeapon:
			case CastSpell:
			case UseWeapon:
			case AttackLoopMarker:
			case BeginPlayerLockout:
			case EndPlayerLockout:
			case IgnoreOtherOpcodes:
			case AttackWithDirectionalProjectile:
			case Hide:
			case Unhide:
			case IfPickedUp:
			case IfTargetInRangeGoto:
			case IfTargetInArcGoto:
			default:
				Console.WriteLine ("Unknown iscript opcode: {0}", buf[pc-1]);
				break;
			}

			return true;
		}
	}

}
