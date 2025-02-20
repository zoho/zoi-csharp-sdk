using Com.Zoho.Officeintegrator.Util;
using System.Collections.Generic;

namespace Com.Zoho.Officeintegrator.V1
{

	public class MergeFieldsResponse : Model, WriterResponseHandler
	{
		private List<MergeFields> merge;
		private Dictionary<string, int?> keyModified=new Dictionary<string, int?>();

		public List<MergeFields> Merge
		{
			/// <summary>The method to get the merge</summary>
			/// <returns>Instance of List<MergeFields></returns>
			get
			{
				return  this.merge;

			}
			/// <summary>The method to set the value to merge</summary>
			/// <param name="merge">Instance of List<MergeFields></param>
			set
			{
				 this.merge=value;

				 this.keyModified["merge"] = 1;

			}
		}

		/// <summary>The method to check if the user has modified the given key</summary>
		/// <param name="key">string</param>
		/// <returns>int? representing the modification</returns>
		public int? IsKeyModified(string key)
		{
			if((( this.keyModified.ContainsKey(key))))
			{
				return  this.keyModified[key];

			}
			return null;


		}

		/// <summary>The method to mark the given key as modified</summary>
		/// <param name="key">string</param>
		/// <param name="modification">int?</param>
		public void SetKeyModified(string key, int? modification)
		{
			 this.keyModified[key] = modification;


		}


	}
}