using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Neosmartpen.Net
{
	public class PenProfile
	{
		public static readonly int LIMIT_BYTE_LENGTH_PROFILE_NAME = 8;
		public static readonly int LIMIT_BYTE_LENGTH_PASSWORD = 8;
		public static readonly int LIMIT_BYTE_LENGTH_KEY = 16;

		public static readonly int LIMIT_BYTE_LENGTH_PEN_NAME = 72;
		public static readonly int LIMIT_BYTE_LENGTH_PEN_STROKE_THICKNESS = 1;
		public static readonly int LIMIT_BYTE_LENGTH_PEN_COLOR_INDEX = 1;
		public static readonly int LIMIT_BYTE_LENGTH_PEN_COLOR_AND_HISTORY = 4 * 5 * 10;
		public static readonly int LIMIT_BYTE_LENGTH_USER_CALIBRATION = 2 * 2 * 3;
		public static readonly int LIMIT_BYTE_LENGTH_PEN_BRUSH_TYPE = 1;
		public static readonly int LIMIT_BYTE_LENGTH_PEN_TIP_TYPE = 1;

		/////////////////
		// Key
		public static readonly string KEY_PEN_NAME = "N_name";
		public static readonly string KEY_PEN_STROKE_THICKNESS_LEVEL = "N_thickness";
		public static readonly string KEY_PEN_COLOR_INDEX = "N_color_index";
		public static readonly string KEY_PEN_COLOR_AND_HISTORY = "N_color";
		public static readonly string KEY_USER_CALIBRATION = "N_pressure";
		public static readonly string KEY_PEN_BRUSH_TYPE = "N_brush";
		public static readonly string KEY_PEN_TIP_TYPE = "N_pt_change1";


		/////////////////
		// request type
		public static readonly byte PROFILE_CREATE = 0x01;
		public static readonly byte PROFILE_DELETE = 0x02;
		public static readonly byte PROFILE_INFO = 0x03;
		public static readonly byte PROFILE_READ_VALUE = 0x12;
		public static readonly byte PROFILE_WRITE_VALUE = 0x11;
		public static readonly byte PROFILE_DELETE_VALUE = 0x13;



		///////////////////
		// status
		public static readonly byte PROFILE_STATUS_SUCCESS = 0x00;
		public static readonly byte PROFILE_STATUS_FAILURE = 0x01;
		public static readonly byte PROFILE_STATUS_EXIST_PROFILE_ALREADY = 0x10;
		public static readonly byte PROFILE_STATUS_NO_EXIST_PROFILE = 0x11;
		//    public static readonly byte PROFILE_STATUS_EXIST_KEY_ALREADY = 0x20;
		public static readonly byte PROFILE_STATUS_NO_EXIST_KEY = 0x21;
		public static readonly byte PROFILE_STATUS_NO_PERMISSION = 0x30;
		public static readonly byte PROFILE_STATUS_BUFFER_SIZE_ERR = 0x40;
	}
}
