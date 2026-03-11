using System;
using UnityEngine;

namespace RuntimeInspectorNamespace
{
	public class ExpressionField : InspectorField
	{
		public enum Mode { OnValueChange = 0, OnSubmit = 1 };

		[SerializeField]
		private BoundInputField input;

		ExpParser parser;

		private Mode m_setterMode = Mode.OnSubmit;
		public Mode SetterMode
		{
			get { return m_setterMode; }
			set { m_setterMode = value; }
		}

		public override void Initialize()
		{
			base.Initialize();

			input.Initialize();
			input.OnValueChanged += OnValueChanged;
			input.OnValueSubmitted += OnValueSubmitted;
			input.DefaultEmptyValue = string.Empty;
		}

		public override bool SupportsType( Type type )
		{
			return type == typeof(ExpressionData);
		}

		protected override void OnBound()
		{
			base.OnBound();

			var data = (ExpressionData)Value;
			parser = new ExpParser(data);
			input.Text = data.source;
		}

		bool ParseValue(string input, bool test = false) {
			var data = (ExpressionData)Value;
			if(input == "") {
				if(test) return true;
				data.source = input;
				return true;
			}
			parser.SetString(input);
			var newExp = parser.Parse();
			if(newExp == null) return false;
			if(data.isEquation != newExp.IsEquation()) {
				return false;
			}
			if(!test) {
				data.source = input;
			}
			return true;
		}

		private bool OnValueChanged( BoundInputField source, string input )
		{
			return ParseValue(input, test: m_setterMode != Mode.OnValueChange);
		}

		private bool OnValueSubmitted( BoundInputField source, string input ) {
			if( m_setterMode == Mode.OnSubmit ) {
				return ParseValue(input);
			}

			return true;
		}

		protected override void OnSkinChanged()
		{
			base.OnSkinChanged();
			input.Skin = Skin;
		}

		public override void Refresh()
		{
			base.Refresh();

			if( Value == null )
				input.Text = string.Empty;
			else
				input.Text = ((ExpressionData)Value).source;
		}
	}
}