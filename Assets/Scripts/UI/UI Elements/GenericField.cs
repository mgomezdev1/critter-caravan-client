using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Extensions;
using static Utils.GenericUtils;
using JetBrains.Annotations;
using static Networking.ServerAPI.Levels;
using Utils;

#nullable enable
[UxmlElement]
public abstract partial class DataField : VisualElement
{
    protected readonly Label label;
    protected readonly Label errorLabel;

    [UxmlAttribute]
    public string LabelText
    {
        get { return label.text; }
        set { label.text = value; }
    }

    [UxmlAttribute]
    public string ErrorText
    {
        get { return errorLabel.text; }
        set
        {
            errorLabel.text = value;
            UpdateValidState();
        }
    }

    protected VisualElement FieldRow { get; set; }
    public override VisualElement contentContainer => FieldRow;

    public event Action? OnSubmit;

    protected virtual bool BoxInnerField => true;

    public DataField()
    {
        // Initialization of internal values
        FieldRow = new VisualElement();
        FieldRow.AddToClassList("field-row");
        this.AddToClassList("field");

        // Initialization of children
        label = new Label { pickingMode = PickingMode.Ignore };
        label.AddToClassList("field-label");
        errorLabel = new Label();
        errorLabel.AddToClassList("field-error");
        errorLabel.style.display = DisplayStyle.None;

        // Initialization of inner field elements
        VisualElement fieldContainer = FieldRow;
        if (BoxInnerField)
        {
            fieldContainer = new();
            fieldContainer.AddToClassList("field-container");
            FieldRow.Add(fieldContainer);
        }
        foreach (var field in BuildInnerField(fieldContainer)) {
            field.AddToClassList("field-input");
            fieldContainer.Add(field);
        }

        // Attaching children to own component tree
        this.hierarchy.Add(FieldRow);
        this.hierarchy.Add(errorLabel);
        // Adding the absolutely-positioned vowel last for it to render above
        this.hierarchy.Add(label);

        SetValidState(true);
        SetEmptyState(true);
    }

    protected abstract IEnumerable<VisualElement> BuildInnerField(VisualElement parent);

    protected void HandleChangeEvent(ChangeEvent<string> evt)
    {
        string rawValue = evt.newValue;
        SetEmptyState(string.IsNullOrEmpty(rawValue));
        HandleRawValue(rawValue);
    }
    protected abstract void HandleRawValue(string rawValue);

    protected void UpdateValidState()
    {
        SetValidState(string.IsNullOrEmpty(ErrorText));
    }
    protected void SetValidState(bool valid)
    {
        errorLabel.style.display = valid ? DisplayStyle.None : DisplayStyle.Flex;
        this.EnableInClassList("invalid", !valid);
    }
    protected void SetEmptyState(bool empty)
    {
        this.EnableInClassList("empty-field", empty);
    }

    public virtual void Submit()
    {
        OnSubmit?.Invoke();
    }
}

#nullable enable
[UxmlElement]
public abstract partial class GenericField<T> : DataField
{
    public abstract T? Value { get; set; }
    public T? LastValidValue { get; protected set; }

    public Func<string, ValidationResult<T>> Validator
    {
        get { return validator; }
        set { validator = value; }
    }
    protected Func<string, ValidationResult<T>> validator = Validation.RejectAll<T>;

    public virtual event Action<T?>? OnValueChanged;
    public virtual event Action<T?>? OnSubmitValue;

    protected override void HandleRawValue(string rawValue)
    {
        if (validator == null)
        {
            Debug.LogWarning($"Field {this.name} has no validator. Value will always be default.");
            return;
        }

        ValidationResult<T> validationResult = validator(rawValue);
        if (validationResult.Success)
        {
            T result = validationResult.Value
                ?? throw new Exception($"Validator succeeded but returned a null value from raw value \"{rawValue}\"");
            LastValidValue = result;
            OnValueChanged?.Invoke(result);
            ErrorText = "";
        }
        else
        {
            if (string.IsNullOrEmpty(validationResult.Message))
            {
                throw new Exception($"Validator rejected value \"{rawValue}\" but didn't specify a reason");
            }
            ErrorText = validationResult.Message ?? "";
        }
    }

    public override void Submit()
    {
        base.Submit();
        OnSubmitValue?.Invoke(LastValidValue);
    }

    protected void NotifyValueChanged(T? newValue)
    {
        OnValueChanged?.Invoke(newValue);
    }
}

public class TextGenericField<T> : GenericField<T>
{
    public TextField InnerField { get; private set; } = null!; // Assigned in BuildInnerField

    [UxmlAttribute]
    public virtual string TextValue
    {
        get { return InnerField.value; }
        set { InnerField.value = value; }
    }

    [UxmlAttribute]
    public bool HideInput
    {
        get { return InnerField.textEdition.isPassword; }
        set { InnerField.textEdition.isPassword = value; }
    }

    public override T? Value { 
        get => LastValidValue;
        set { 
            LastValidValue = value; 
            InnerField.SetValueWithoutNotify(value?.ToString() ?? "");
            NotifyValueChanged(value);
        } 
    }

    protected override IEnumerable<VisualElement> BuildInnerField(VisualElement parent)
    {
        InnerField = new();
        InnerField.RegisterValueChangedCallback(HandleChangeEvent);
        InnerField.RegisterCallback<KeyDownEvent>(HandleKeyDownEvent, TrickleDown.TrickleDown);
        yield return InnerField;
    }

    private void HandleKeyDownEvent(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Return && !evt.shiftKey)
        {
            Submit();
        }
    }
}

[UxmlElement]
public partial class DropdownField<T> : GenericField<T>
{
    public DropdownField InnerField { get; set; } = null!;

    public override T? Value 
    { 
        get => InnerField.index < 0 ? default : choices[InnerField.index].Value;
        set { LastValidValue = value; TrySelectValue(value); }
    }

    public int Index
    {
        get => InnerField.index;
        set => InnerField.index = value;
    }

    [UxmlAttribute]
    public List<string> ChoiceNames
    {
        get => choices.Select(KvpKey).ToList();
        set
        {
            while (Choices.Count < value.Count)
            {
                Choices.Add(new SerializableKeyValuePair<string, T>("", default!));
            }
            for (int i = 0; i < value.Count; i++)
            {
                Choices[i].Key = value[i];
            }
        }
    }
    [UxmlAttribute]
    public List<T> ChoiceValues
    {
        get => choices.Select(KvpValue).ToList();
        set
        {
            while (Choices.Count < value.Count)
            {
                Choices.Add(new SerializableKeyValuePair<string, T>("", default!));
            }
            for (int i = 0; i < value.Count; i++)
            {
                Choices[i].Value = value[i];
            }
        }
    } 

    public List<SerializableKeyValuePair<string, T>> Choices { 
        get => choices;
        set { 
            List<string> names = value.Select(KvpKey).ToList();
            if (names.HasDuplicates(out string? dupEntry)) 
                Debug.LogWarning($"Choices for {name} contain duplicate entry: \"{dupEntry}\"");
            InnerField.choices = names;
            choices = value;
        }
    }
    private List<SerializableKeyValuePair<string, T>> choices = new();

    public DropdownField() : base()
    {
        // This field always contains some value
        SetEmptyState(false);
    }

    protected override IEnumerable<VisualElement> BuildInnerField(VisualElement parent)
    {
        InnerField = new();
        InnerField.RegisterValueChangedCallback(HandleValueChanged);
        yield return InnerField;
    }

    public void HandleValueChanged(ChangeEvent<string> evt)
    {
        SetSelectionByName(evt.newValue);
    }
    public void SetSelectionByName(string name)
    {
        int index = choices.Select(KvpKey).FindIndex(name);
        if (index != InnerField.index) InnerField.index = index;
        LastValidValue = choices[index].Value;
        NotifyValueChanged(LastValidValue);
    }

    public void TrySelectValue(T? value)
    {
        InnerField.index = choices.Select(KvpValue).FindIndex(value);
    }
}

[UxmlElement]
public partial class CheckboxField : GenericField<bool>
{
    public Toggle InnerField { get; set; } = null!;

    public override bool Value { 
        get => InnerField.value; 
        set => InnerField.value = value; 
    }

    public CheckboxField() : base()
    {
        SetEmptyState(false);
    }

    protected override IEnumerable<VisualElement> BuildInnerField(VisualElement parent)
    {
        parent.style.justifyContent = Justify.Center;
        InnerField = new Toggle();
        InnerField.RegisterValueChangedCallback(HandleValueChanged);
        yield return InnerField;
    }

    public void HandleValueChanged(ChangeEvent<bool> evt)
    {
        LastValidValue = evt.newValue;
        NotifyValueChanged(LastValidValue);
    }
}

[UxmlElement]
public partial class ButtonField : GenericField<bool>
{
    public Button InnerField { get; set; } = null!;

    private bool isPressed = false;
    public event Action? OnClick;

    public override bool Value
    {
        get => isPressed;
        set => throw new InvalidOperationException("The value of a Button Field cannot be set");
    }

    public ButtonField() : base()
    {
        SetEmptyState(false);
    }

    protected override IEnumerable<VisualElement> BuildInnerField(VisualElement parent)
    {
        InnerField = new Button();
        InnerField.RegisterCallback<MouseDownEvent>(HandleMouseDown, TrickleDown.TrickleDown);
        InnerField.RegisterCallback<MouseLeaveEvent>(HandleMouseLeave, TrickleDown.TrickleDown);
        InnerField.RegisterCallback<MouseUpEvent>(HandleMouseUp, TrickleDown.TrickleDown);
        InnerField.clicked += HandleClick;
        yield return InnerField;
    }

    protected virtual void HandleClick()
    {
        isPressed = false; // a click happens when the mouse is released
        OnClick?.Invoke();
    }
    protected void HandleMouseDown(MouseDownEvent evt)
    {
        if (evt.button != 0) return;
        SetPressedValue(true);
    }
    protected void HandleMouseLeave(MouseLeaveEvent evt)
    {
        SetPressedValue(false);
    }
    protected void HandleMouseUp(MouseUpEvent evt)
    {
        if (evt.button != 0) return;
        SetPressedValue(false);
    }
    protected void SetPressedValue(bool value)
    {
        if (isPressed == value) return;
        isPressed = value;
        LastValidValue = value;
        NotifyValueChanged(value);
    }
}

[UxmlElement]
public partial class ToggleField : ButtonField
{
    private bool state;
    public override bool Value { get => state; set => SetState(value); }

    public VisualElement ToggleHandle { get; set; } = null!;

    protected override IEnumerable<VisualElement> BuildInnerField(VisualElement parent)
    {
        var result = base.BuildInnerField(parent).ToArray(); // run the full enumerable
        ToggleHandle = new() { name = "toggle-handle" };
        InnerField.Add(ToggleHandle);
        return result;
    }

    protected override void HandleClick()
    {
        base.HandleClick();
        SetState(!this.state);
    }

    public void SetState(bool state)
    {
        if (state == this.state) return;
        this.state = state;
        this.EnableInClassList("toggled", state);
        NotifyValueChanged(state);
    }
}

[UxmlElement]
public partial class EnumDropdown<T> : DropdownField<T> where T : Enum
{
    public EnumDropdown()
    {
        Array enumVals = Enum.GetValues(typeof(T));
        List<SerializableKeyValuePair<string, T>> choices = new();
        foreach (var val in enumVals)
        {
            choices.Add(new SerializableKeyValuePair<string, T>(val.ToString(), (T)val));
        }
        Choices = choices;
    }
}

[UxmlElement]
public partial class SortCriterionDropdown : EnumDropdown<SortCriterion> { }

[UxmlElement]
public partial class StringField : TextGenericField<string>
{
    public StringField() : base()
    {
        validator = Validation.AllowAll;
    }
}

[UxmlElement]
public partial class IntField : TextGenericField<int>
{
    public IntField() : base()
    {
        InnerField.textEdition.keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
        validator = Validation.Integer;
    }
}

[UxmlElement]
public partial class NumericField : TextGenericField<float>
{
    public NumericField() : base()
    {
        InnerField.keyboardType = TouchScreenKeyboardType.NumbersAndPunctuation;
        validator = Validation.Numeric;
    }
}