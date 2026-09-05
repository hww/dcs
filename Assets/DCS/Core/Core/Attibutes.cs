using UnityEngine;

namespace DynamicComponent
{
	/// <summary>
	/// Base attribute for configuring component pool memory layout.
	/// </summary>
	[System.AttributeUsage(System.AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
	public class BasePoolAttribute : System.Attribute
	{
		/// <summary>Initial pool capacity (maximum concurrent instances).</summary>
		public int Capacity { get; }

		/// <summary>Bit mask for group filtering (pause, categories).</summary>
		public uint Mask { get; }

		/// <summary>Update stages on the main processor (PPU).</summary>
		public EUpdateStage UpdateStage { get; }

		/// <summary>Asynchronous update stages (SPU / Job System).</summary>
		public EAsyncUpdateStage AsyncUpdateStage { get; }

		public BasePoolAttribute(
			int capacity = 1000,
			EUpdateStage updateStage = EUpdateStage.Update,
			EAsyncUpdateStage asyncUpdateStage = EAsyncUpdateStage.None,
			uint mask = 0)
		{
			Capacity = capacity;
			UpdateStage = updateStage;
			AsyncUpdateStage = asyncUpdateStage;
			Mask = mask;
		}
	}

	/// <summary>
	/// Attribute for persistent components (data, physics, transforms).
	/// </summary>
	public class ComponentPoolAttribute : BasePoolAttribute
	{
		public ComponentPoolAttribute(
			int capacity = 1000,
			EUpdateStage updateStage = EUpdateStage.Update,
			EAsyncUpdateStage asyncUpdateStage = EAsyncUpdateStage.None,
			uint mask = 0)
			: base(capacity, updateStage, asyncUpdateStage, mask) { }
	}

	/// <summary>
	/// Attribute for event components (messages, signals).
	/// </summary>
	public class MessagePoolAttribute : BasePoolAttribute
	{
		public MessagePoolAttribute(
			int capacity = 1000,
			EUpdateStage updateStage = EUpdateStage.Update,
			EAsyncUpdateStage asyncUpdateStage = EAsyncUpdateStage.None,
			uint mask = 0)
			: base(capacity, updateStage, asyncUpdateStage, mask) { }
	}
}