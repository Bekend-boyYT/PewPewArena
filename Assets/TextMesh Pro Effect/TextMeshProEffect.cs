// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace
#pragma warning disable 0649
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace TMPro
{
	[RequireComponent(typeof(TMP_Text))]
	[ExecuteInEditMode]
	public class TextMeshProEffect : MonoBehaviour
	{
		public enum EffectType : byte
		{
			Waves,
			Grow,
			Unfold,
			UnfoldAndWaves,
			Sketch,
			Bounce
		}

		public enum MixType : byte
		{
			Multiply,
			Add
		}

		[ExecuteInEditMode]
		internal class SharedState : MonoBehaviour
		{
			[NonSerialized]
			internal bool TextMeshIsUpdated;

			private void LateUpdate()
			{
				TextMeshIsUpdated = false;
			}
		}

		[Serializable]
		private sealed class о
		{
			public static readonly о ò = new о();

			public static Func<TextMeshProEffect, bool> ó;

			internal bool ô(TextMeshProEffect ö)
			{
				return ö == null || !ö.enabled;
			}
		}

		[StructLayout(LayoutKind.Auto)]
		private struct Ö
		{
			public TMP_CharacterInfo º;

			public TextMeshProEffect Ç;
		}

		[StructLayout(LayoutKind.Auto)]
		private struct ü
		{
			public float é;

			public TMP_CharacterInfo â;

			public TextMeshProEffect ä;
		}

		[StructLayout(LayoutKind.Auto)]
		private struct à
		{
			public float å;

			public TMP_CharacterInfo ç;

			public TextMeshProEffect ê;
		}

		public EffectType Type;

		public float DurationInSeconds = 0.5f;

		public float Amplitude = 1f;

		[Space]
		[Range(0f, 1f)]
		public float CharacterDurationRatio = 0f;

		public int CharactersPerDuration = 0;

		[Space]
		public Gradient Gradient = new Gradient();

		public MixType Mix = MixType.Multiply;

		[Space]
		public bool AutoPlay = true;

		public bool Reverse = false;

		public bool Repeat;

		public string ForWords;

		private readonly List<ValueTuple<int, int>> öæ = new List<ValueTuple<int, int>>();

		[NonSerialized]
		public bool IsFinished;

		private float öÆ;

		private TMP_Text öû;

		private EffectType öù;

		private bool öÿ;

		private bool öÜ;

		private bool öƒ;

		private float öá;

		private string öí;

		private ushort öú;

		private float[] öñ = new float[10];

		private SharedState öÑ;

		private float öª;

		private TMP_TextInfo òo;

		private string òο;

		public List<ValueTuple<int, int>> Intervals => öæ;

		private SharedState SharedStateProp
		{
			get
			{
				if (öÑ != null)
				{
					return öÑ;
				}
				öÑ = GetComponent<SharedState>();
				if (öÑ == null)
				{
					öÑ = base.gameObject.AddComponent<SharedState>();
					öÑ.hideFlags = HideFlags.HideInInspector | HideFlags.DontSaveInEditor | HideFlags.NotEditable | HideFlags.DontSaveInBuild;
				}
				return öÑ;
			}
		}

		public void CopyTo(TextMeshProEffect effect)
		{
			effect.Type = Type;
			effect.DurationInSeconds = DurationInSeconds;
			effect.Amplitude = Amplitude;
			effect.CharacterDurationRatio = CharacterDurationRatio;
			effect.CharactersPerDuration = CharactersPerDuration;
			effect.Gradient = Gradient;
			effect.Mix = Mix;
			effect.AutoPlay = AutoPlay;
			effect.Repeat = Repeat;
			effect.ForWords = ForWords;
		}

		public void Apply()
		{
			öû = GetComponent<TMP_Text>();
			öù = Type;
			öÿ = öù == EffectType.Unfold || öù == EffectType.Grow || öù == EffectType.Bounce;
			öÜ = öù == EffectType.Sketch;
			öƒ = false;
			öá = -1f;
		}

		private void OnEnable()
		{
			if (AutoPlay)
			{
				Play();
			}
		}

		private void OnDestroy()
		{
			öÑ = GetComponent<SharedState>();
			if (!(öÑ == null))
			{
				TextMeshProEffect[] components = base.gameObject.GetComponents<TextMeshProEffect>();
				if (components.Length == 0 || components.All(о.ò.ô))
				{
					UnityEngine.Object.Destroy(öÑ);
				}
			}
		}

		private void OnValidate()
		{
			if (AutoPlay)
			{
				Play();
			}
			else
			{
				Apply();
			}
		}

		private void LateUpdate()
		{
			if ((UnityEngine.Object)(object)öû == null || DurationInSeconds <= 0f || !öƒ)
			{
				return;
			}
			if (Repeat && IsFinished)
			{
				Play();
			}
			if (TMP_Settings.instance == null)
			{
				return;
			}
			if (!SharedStateProp.TextMeshIsUpdated)
			{
				öû.ForceMeshUpdate();
				SharedStateProp.TextMeshIsUpdated = true;
			}
			òo = öû.textInfo;
			if (òo == null || òo.meshInfo == null || òo.meshInfo.Length == 0 || òo.meshInfo[0].vertices == null)
			{
				return;
			}
			TMP_MeshInfo[] array = Array.Empty<TMP_MeshInfo>();
			if (Application.isEditor)
			{
				try
				{
					array = òo.CopyMeshInfoVertexData();
				}
				catch (NullReferenceException ex)
				{
					FieldInfo field = typeof(TMP_TextInfo).GetField("m_CachedMeshInfo", BindingFlags.Instance | BindingFlags.NonPublic);
					if (field != null)
					{
						field.SetValue(òo, null);
					}
					Debug.Log("TMP bug. Workaround applied." + ex, this);
					array = òo.CopyMeshInfoVertexData();
				}
			}
			else
			{
				array = òo.CopyMeshInfoVertexData();
			}
			int characterCount = òo.characterCount;
			if (characterCount == 0)
			{
				IsFinished = true;
				return;
			}
			float num = Time.realtimeSinceStartup - öÆ;
			if (öí != öû.text || ForWords != òο)
			{
				öá = -1f;
				öí = öû.text;
				òο = ForWords;
				ë();
			}
			if (CharactersPerDuration > 0)
			{
				öª = DurationInSeconds * (float)öí.Length / (float)CharactersPerDuration;
			}
			else
			{
				öª = DurationInSeconds;
			}
			if (öÜ && num >= öá)
			{
				öá = num + öª;
				öú++;
				if (öñ.Length < characterCount * 2)
				{
					öñ = new float[characterCount * 2];
				}
				for (int i = 0; i < öñ.Length; i++)
				{
					öñ[i] = UnityEngine.Random.value;
				}
			}
			if (öÿ && num > öª)
			{
				num = öª;
				IsFinished = true;
			}
			float num2 = num / öª;
			if (!öÿ)
			{
				num2 %= 1f;
			}
			float num3 = num2;
			if (Reverse)
			{
				num3 = 1f - num2;
			}
			float characterDurationRatio = CharacterDurationRatio;
			float num4 = Mathf.Lerp(1f / (float)characterCount, 1f, characterDurationRatio);
			int num5 = 0;
			int num6 = characterCount;
			if (öæ.Count > 0 || !string.IsNullOrEmpty(ForWords))
			{
				num6 = 0;
				for (int j = 0; j < öæ.Count; j++)
				{
					ValueTuple<int, int> valueTuple = öæ[j];
					num6 += valueTuple.Item2 - valueTuple.Item1 + 1;
				}
			}
			for (int k = 0; k < characterCount; k++)
			{
				if (öæ.Count > 0 || !string.IsNullOrEmpty(ForWords))
				{
					bool flag = false;
					for (int l = 0; l < öæ.Count; l++)
					{
						ValueTuple<int, int> valueTuple2 = öæ[l];
						if (k >= valueTuple2.Item1 && k <= valueTuple2.Item2)
						{
							flag = true;
						}
					}
					if (!flag)
					{
						continue;
					}
				}
				TMP_CharacterInfo ä = òo.characterInfo[k];
				if (ä.isVisible)
				{
					float num7 = Mathf.Lerp((float)num5 * 1f / (float)num6, 0f, characterDurationRatio);
					float value = (num3 - num7) / num4;
					value = Mathf.Clamp01(value);
					int materialReferenceIndex = ä.materialReferenceIndex;
					int vertexIndex = ä.vertexIndex;
					Color32[] colors = òo.meshInfo[materialReferenceIndex].colors32;
					Vector3[] vertices = array[materialReferenceIndex].vertices;
					Vector3[] vertices2 = òo.meshInfo[materialReferenceIndex].vertices;
					î(òo, ä, vertexIndex, colors, vertices2, vertices, num3, value, öú);
					num5++;
				}
			}
			for (int m = 0; m < òo.meshInfo.Length; m++)
			{
				if (m < òo.materialCount)
				{
					òo.meshInfo[m].mesh.vertices = òo.meshInfo[m].vertices;
					öû.UpdateGeometry(òo.meshInfo[m].mesh, m);
				}
			}
			öû.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
		}

		private void ë()
		{
			öæ.Clear();
			if (string.IsNullOrWhiteSpace(ForWords) || öí == null)
			{
				return;
			}
			StringBuilder stringBuilder = new StringBuilder(òo.characterCount);
			for (int i = 0; i < òo.characterCount; i++)
			{
				stringBuilder.Append(òo.characterInfo[i].character);
			}
			bool flag = (öû.fontStyle & (FontStyles.LowerCase | FontStyles.UpperCase | FontStyles.SmallCaps)) != 0;
			string text = stringBuilder.ToString();
			string[] array = ForWords.Split(new char[2] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = array;
			foreach (string text2 in array2)
			{
				int startIndex = 0;
				while (true)
				{
					startIndex = text.IndexOf(text2, startIndex, flag ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
					if (startIndex == -1)
					{
						break;
					}
					if (startIndex <= 0 || è(text[startIndex - 1]))
					{
						int num = startIndex + text2.Length;
						if (num >= text.Length || è(text[num]))
						{
							öæ.Add(new ValueTuple<int, int>(startIndex, startIndex + text2.Length - 1));
						}
					}
					startIndex += text2.Length;
				}
			}
		}

		private bool è(char ï)
		{
			return char.IsWhiteSpace(ï) || char.IsSeparator(ï) || char.IsPunctuation(ï);
		}

		private void î(TMP_TextInfo ì, TMP_CharacterInfo Ä, int Å, Color32[] É, Vector3[] æ, Vector3[] Æ, float û, float ù, ushort ÿ)
		{
			if (öÜ)
			{
				οο(Ä, Å, É, Ü(öú + Ä.index));
			}
			else
			{
				οο(Ä, Å, É, ù);
			}
			switch (Type)
			{
			case EffectType.Waves:
				οó(Ä, Å, æ, Æ, û);
				break;
			case EffectType.Grow:
				οë(Ä, Å, æ, Æ, ù);
				break;
			case EffectType.Unfold:
				οÅ(Ä, Å, æ, Æ, ù);
				break;
			case EffectType.UnfoldAndWaves:
				οÅ(Ä, Å, æ, Æ, ù);
				οó(Ä, Å, æ, æ, û);
				break;
			case EffectType.Sketch:
				á(Ä, Å, æ, Æ, ù, ÿ);
				break;
			case EffectType.Bounce:
				οâ(Ä, Å, æ, Æ, ù);
				break;
			default:
				throw new ArgumentOutOfRangeException();
			}
		}

		private float Ü(int ƒ)
		{
			int num = Mathf.Abs(ƒ % öñ.Length);
			return öñ[num];
		}

		private void á(TMP_CharacterInfo í, int ú, Vector3[] ñ, Vector3[] Ñ, float ª, int οo)
		{
			Ö οá = default(Ö);
			οá.º = í;
			οá.Ç = this;
			ñ[ú] = Ñ[ú] - οÿ(ú, οo, ref οá);
			ñ[ú + 1] = Ñ[ú + 1] - οÿ(ú + 1, οo, ref οá);
			ñ[ú + 2] = Ñ[ú + 2] - οÿ(ú + 2, οo, ref οá);
			ñ[ú + 3] = Ñ[ú + 3] - οÿ(ú + 3, οo, ref οá);
		}

		private void οο(TMP_CharacterInfo οо, int οô, Color32[] οö, float οò)
		{
			Color color = Gradient.Evaluate(οò);
			if (Mix == MixType.Multiply)
			{
				ref Color32 reference = ref οö[οô];
				reference *= color;
				ref Color32 reference2 = ref οö[οô + 1];
				reference2 *= color;
				ref Color32 reference3 = ref οö[οô + 2];
				reference3 *= color;
				ref Color32 reference4 = ref οö[οô + 3];
				reference4 *= color;
			}
			else
			{
				for (int i = 0; i < 4; i++)
				{
					Color color2 = οö[οô + i] + color;
					color2.a *= color.a;
					οö[οô + i] = color2;
				}
			}
		}

		private void οó(TMP_CharacterInfo οÖ, int οº, Vector3[] οÇ, Vector3[] οü, float οé)
		{
			ü οñ = default(ü);
			οñ.é = οé;
			οñ.â = οÖ;
			οñ.ä = this;
			οÇ[οº] = οü[οº] - οí(οº, ref οñ);
			οÇ[οº + 1] = οü[οº + 1] - οí(οº + 1, ref οñ);
			οÇ[οº + 2] = οü[οº + 2] - οí(οº + 2, ref οñ);
			οÇ[οº + 3] = οü[οº + 3] - οí(οº + 3, ref οñ);
		}

		private void οâ(TMP_CharacterInfo οä, int οà, Vector3[] οå, Vector3[] οç, float οê)
		{
			à оo = default(à);
			оo.å = οê;
			оo.ç = οä;
			оo.ê = this;
			οå[οà] = οç[οà] - οÑ(οà, ref оo);
			οå[οà + 1] = οç[οà + 1] - οÑ(οà + 1, ref оo);
			οå[οà + 2] = οç[οà + 2] - οÑ(οà + 2, ref оo);
			οå[οà + 3] = οç[οà + 3] - οÑ(οà + 3, ref оo);
		}

		private void οë(TMP_CharacterInfo οè, int οï, Vector3[] οî, Vector3[] οì, float οÄ)
		{
			οî[οï] = οì[οï];
			οî[οï + 3] = οì[οï + 3];
			οî[οï + 1] = Vector3.Lerp(οì[οï], οì[οï + 1], οÄ);
			οî[οï + 2] = Vector3.Lerp(οì[οï + 3], οì[οï + 2], οÄ);
			οî[οï] = Vector3.LerpUnclamped(οì[οï], οî[οï], Amplitude);
			οî[οï + 1] = Vector3.LerpUnclamped(οì[οï + 1], οî[οï + 1], Amplitude);
			οî[οï + 2] = Vector3.LerpUnclamped(οì[οï + 2], οî[οï + 2], Amplitude);
			οî[οï + 3] = Vector3.LerpUnclamped(οì[οï + 3], οî[οï + 3], Amplitude);
		}

		private void οÅ(TMP_CharacterInfo οÉ, int οæ, Vector3[] οÆ, Vector3[] οû, float οù)
		{
			Vector3 a = (οû[οæ] + οû[οæ + 1]) * 0.5f;
			Vector3 a2 = (οû[οæ + 3] + οû[οæ + 2]) * 0.5f;
			οÆ[οæ] = Vector3.Lerp(a, οû[οæ], οù);
			οÆ[οæ + 3] = Vector3.Lerp(a2, οû[οæ + 3], οù);
			οÆ[οæ + 1] = Vector3.Lerp(a, οû[οæ + 1], οù);
			οÆ[οæ + 2] = Vector3.Lerp(a2, οû[οæ + 2], οù);
			οÆ[οæ] = Vector3.LerpUnclamped(οû[οæ], οÆ[οæ], Amplitude);
			οÆ[οæ + 1] = Vector3.LerpUnclamped(οû[οæ + 1], οÆ[οæ + 1], Amplitude);
			οÆ[οæ + 2] = Vector3.LerpUnclamped(οû[οæ + 2], οÆ[οæ + 2], Amplitude);
			οÆ[οæ + 3] = Vector3.LerpUnclamped(οû[οæ + 3], οÆ[οæ + 3], Amplitude);
		}

		[ContextMenu("Play")]
		public void Play()
		{
			Apply();
			IsFinished = false;
			öÆ = Time.realtimeSinceStartup;
			öƒ = true;
		}

		[ContextMenu("Finish")]
		public void Finish()
		{
			öÆ = float.MinValue;
		}

		private Vector3 οÿ(int οÜ, int οƒ, ref Ö οá)
		{
			float num = οá.º.pointSize * 0.1f * Amplitude;
			float num2 = Ü(οÜ << οƒ);
			float num3 = Ü(οÜ << οƒ >> 5);
			return new Vector3(num2 * num, num3 * num, 0f);
		}

		private Vector3 οí(int οú, ref ü οñ)
		{
			float f = MathF.PI * -2f * οñ.é + (float)(οú / 4) * 0.3f;
			return new Vector3(0f, Mathf.Cos(f) * οñ.â.pointSize * 0.3f * Amplitude, 0f);
		}

		private Vector3 οÑ(int οª, ref à оo)
		{
			float f = MathF.PI * -2f * оo.å;
			return new Vector3(0f, Mathf.Cos(f) * оo.ç.pointSize * 0.3f * Amplitude, 0f);
		}
	}
}
