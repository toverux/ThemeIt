// History from oldest to newest:
// * SamSamTS's Building Themes
// * AJ3D's Ploppable RICO
// * algernon's Ploppable RICO Revisited (added FindBuilding())
// * toverux's ThemeIt (removed FindBuilding(), reformatted, fixed all kinds of warnings automatically)

using System;
using ColossalFramework.UI;
using UnityEngine;

namespace ThemeIt.ThirdParty;

public interface IUIFastListRow {
    #region Methods to implement

    /// <summary>
    /// Method invoked very often, make sure it is fast
    /// Avoid doing any calculations, the data should be already processed any ready to display.
    /// </summary>
    /// <param name="data">What needs to be displayed</param>
    /// <param name="isRowOdd">Use this to display a different look for your odd rows</param>
    void Display(object data, bool isRowOdd);

    /// <summary>
    /// Change the style of the selected row here
    /// </summary>
    /// <param name="isRowOdd">Use this to display a different look for your odd rows</param>
    void Select(bool isRowOdd);

    /// <summary>
    /// Change the style of the row back from selected here
    /// </summary>
    /// <param name="isRowOdd">Use this to display a different look for your odd rows</param>
    void Deselect(bool isRowOdd);

    #endregion

    #region From UIPanel

    // No need to implement those, they are in UIPanel
    // Those are declared here so they can be used inside UIFastList
    // ReSharper disable InconsistentNaming
    float width { get; set; }
    bool enabled { get; set; }
    Vector3 relativePosition { get; set; }
    event MouseEventHandler eventClick;
    event MouseEventHandler eventMouseEnter;
    // ReSharper restore InconsistentNaming

    #endregion
}

/// <summary>
/// This component is specifically designed the handle the display of
/// very large amount of rows in a scrollable panel while minimizing
/// the impact on the performances.
///
/// This class will instantiate the rows for you based on the actual
/// height of the UIFastList and the rowHeight value provided.
///
/// The row class must inherit UIPanel and implement IUIFastListRow :
/// public class MyCustomRow : UIPanel, IUIFastListRow
///
/// How it works :
/// This class only instantiate as many rows as visible on screen (+1
/// extra to simulate in-between steps). Then the content of those is
/// updated according to what needs to be displayed by calling the
/// Display method declared in IUIFastListRow.
///
/// Provide the list of data with rowData. This data is send back to
/// your custom row when it needs to be displayed. For optimal
/// performances, make sure this data is already processed and ready
/// to display.
///s
/// Creation example :
/// UIFastList myFastList = UIFastList.Create>MyCustomRow>(this);
/// myFastList.size = new Vector2(200f, 300f);
/// myFastList.rowHeight = 40f;
/// myFastList.rowData = myDataList;
///
/// </summary>
public class UIFastList : UIComponent {
    #region Private members

    private UIPanel? panel;
    private UIScrollbar? scrollbar;
    private FastList<IUIFastListRow>? rows;
    private FastList<object>? mRowsData;

    private Type? mRowType;
    private string? mBackgroundSprite;
    private Color32 mColor = new(255, 255, 255, 255);
    private float mRowHeight = -1;
    private float mPos = -1;
    private float mStepSize;
    private bool mCanSelect;
    private int mSelectedDataId = -1;
    private int mSelectedRowId = -1;
    private bool mLock;
    private bool mUpdateContent = true;
    private bool mAutoHideScrollbar;
    private UIComponent? mLastMouseEnter;

    #endregion

    /// <summary>
    /// Use this to create the UIFastList.
    /// Do NOT use AddUIComponent.
    /// I had to do that way because MonoBehaviors classes cannot be generic
    /// </summary>
    /// <typeparam name="T">The type of the row UI component</typeparam>
    /// <param name="parent"></param>
    /// <returns></returns>
    public static UIFastList Create<T>(UIComponent parent)
        where T : UIPanel, IUIFastListRow {
        var list = parent.AddUIComponent<UIFastList>();
        list.mRowType = typeof(T);
        return list;
    }

    #region Public accessors

    public bool AutoHideScrollbar {
        get => this.mAutoHideScrollbar;
        set {
            if (this.mAutoHideScrollbar != value) {
                this.mAutoHideScrollbar = value;
                this.UpdateScrollbar();
            }
        }
    }

    /// <summary>
    /// Change the color of the background
    /// </summary>
    public Color32 BackgroundColor {
        get => this.mColor;
        set {
            this.mColor = value;
            if (this.panel != null)
                this.panel.color = value;
        }
    }

    /// <summary>
    /// Change the sprite of the background
    /// </summary>
    public string? BackgroundSprite {
        get => this.mBackgroundSprite;
        set {
            if (this.mBackgroundSprite != value) {
                this.mBackgroundSprite = value;
                if (this.panel != null)
                    this.panel.backgroundSprite = value;
            }
        }
    }

    /// <summary>
    /// Can rows be selected by clicking on them
    /// Default value is false
    /// Rows can still be selected via selectedIndex
    /// </summary>
    public bool CanSelect {
        get => this.mCanSelect;
        set {
            if (this.mCanSelect != value) {
                this.mCanSelect = value;

                if (this.rows == null) return;
                for (var i = 0; i < this.rows.m_size; i++) {
                    if (this.mCanSelect)
                        this.rows[i].eventClick += this.OnRowClicked;
                    else
                        this.rows[i].eventClick -= this.OnRowClicked;
                }
            }
        }
    }

    /// <summary>
    /// Change the position in the list
    /// Display the data at the position in the top row.
    /// This doesn't update the list if the position stay the same
    /// Use DisplayAt for that
    /// </summary>
    public float ListPosition {
        get => this.mPos;
        set {
            if (this.mRowHeight <= 0) return;
            if (Math.Abs(this.mPos - value) > double.Epsilon) {
                var rowsData = this.mRowsData;
                if (rowsData != null) {
                    var pos = Mathf.Max(Mathf.Min(value, rowsData.m_size - this.height / this.mRowHeight), 0);
                    this.mUpdateContent = Mathf.FloorToInt(this.mPos) != Mathf.FloorToInt(pos);
                    this.DisplayAt(pos);
                }
            }
        }
    }

    /// <summary>
    /// This is the list of data that will be send to the IUIFastListRow.Display method
    /// Changing this list will reset the display position to 0
    /// You can also change rowsData.m_buffer and rowsData.m_size
    /// and refresh the display with DisplayAt method
    /// </summary>
    public FastList<object> RowsData {
        get {
            if (this.mRowsData == null) this.mRowsData = new FastList<object>();
            return this.mRowsData;
        }
        set {
            if (this.mRowsData != value) {
                this.mRowsData = value;
                this.DisplayAt(0);
            }
        }
    }

    /// <summary>
    /// This MUST be set, it is the height in pixels of each row
    /// </summary>
    public float RowHeight {
        get => this.mRowHeight;
        set {
            if (Math.Abs(this.mRowHeight - value) > double.Epsilon) {
                this.mRowHeight = value;
                this.CheckRows();
            }
        }
    }

    /// <summary>
    /// Currently selected row
    /// -1 if none selected
    /// </summary>
    public int SelectedIndex {
        get => this.mSelectedDataId;
        set {
            if (this.mRowsData == null || this.mRowsData.m_size == 0) {
                this.mSelectedDataId = -1;
                return;
            }

            var oldId = this.mSelectedDataId;
            if (oldId >= this.mRowsData.m_size) oldId = -1;
            this.mSelectedDataId = Mathf.Min(Mathf.Max(-1, value), this.mRowsData.m_size - 1);

            var pos = Mathf.FloorToInt(this.mPos);
            var newRowId = Mathf.Max(-1, this.mSelectedDataId - pos);
            var fastList = this.rows;
            if (fastList != null && newRowId >= fastList.m_size) newRowId = -1;

            if (newRowId >= 0 && newRowId == this.mSelectedRowId && !this.mUpdateContent) return;

            if (this.mSelectedRowId >= 0) {
                this.rows?[this.mSelectedRowId].Deselect((oldId % 2) == 1);
                this.mSelectedRowId = -1;
            }

            if (newRowId >= 0) {
                this.mSelectedRowId = newRowId;
                this.rows?[this.mSelectedRowId].Select((this.mSelectedDataId % 2) == 1);
            }

            if (this.EventSelectedIndexChanged != null && this.mSelectedDataId != oldId)
                this.EventSelectedIndexChanged(this, this.mSelectedDataId);
        }
    }

    public object? SelectedItem {
        get {
            if (this.mSelectedDataId == -1) return null;
            return this.mRowsData?.m_buffer[this.mSelectedDataId];
        }
    }

    public bool SelectOnMouseEnter { get; set; }

    /// <summary>
    /// The number of pixels moved at each scroll step
    /// When set to 0 or less, rowHeight is used instead.
    /// </summary>
    public float StepSize {
        get => (this.mStepSize > 0) ? this.mStepSize : this.mRowHeight;
        set => this.mStepSize = value;
    }

    #endregion

    #region Events

    /// <summary>
    /// Called when the currently selected row changed
    /// </summary>
    public event PropertyChangedEventHandler<int>? EventSelectedIndexChanged;

    #endregion

    #region Public methods

    /// <summary>
    /// Clear the list
    /// </summary>
    public void Clear() {
        this.mRowsData?.Clear();

        var fastList = this.rows;
        if (fastList != null) {
            for (var i = 0; i < fastList.m_size; i++) {
                fastList[i].enabled = false;
            }
        }

        this.UpdateScrollbar();
    }

    /// <summary>
    /// Display the data at the position in the top row.
    /// This update the list even if the position remind the same
    /// </summary>
    /// <param name="pos">Index position in the list</param>
    public void DisplayAt(float pos) {
        if (this.mRowsData == null || this.mRowHeight <= 0) return;

        this.SetupControls();

        this.mPos = Mathf.Max(Mathf.Min(pos, this.mRowsData.m_size - this.height / this.mRowHeight), 0f);

        var fastList = this.rows;
        if (fastList != null) {
            for (var i = 0; i < fastList.m_size; i++) {
                var dataPos = Mathf.FloorToInt(this.mPos + i);
                var offset = this.RowHeight * (this.mPos + i - dataPos);
                if (dataPos < this.mRowsData.m_size) {
                    if (this.mUpdateContent)
                        fastList[i].Display(this.mRowsData[dataPos], (dataPos % 2) == 1);

                    if (dataPos == this.mSelectedDataId && this.mUpdateContent) {
                        this.mSelectedRowId = i;
                        fastList[this.mSelectedRowId].Select((dataPos % 2) == 1);
                    }

                    fastList[i].enabled = true;
                }
                else
                    fastList[i].enabled = false;

                fastList[i].relativePosition = new Vector3(0, i * this.RowHeight - offset);
            }
        }

        this.UpdateScrollbar();
        this.mUpdateContent = true;
    }

    /// <summary>
    /// Refresh the display
    /// </summary>
    public void Refresh() {
        this.DisplayAt(this.mPos);
    }

    #endregion

    #region Overrides

    public override void Start() {
        base.Start();

        this.SetupControls();
    }

    public override void OnDestroy() {
        base.OnDestroy();

        if (this.panel == null) return;

        Destroy(this.panel);
        Destroy(this.scrollbar);

        if (this.rows == null) return;

        for (var i = 0; i < this.rows.m_size; i++) {
            // ReSharper disable once SuspiciousTypeConversion.Global
            Destroy(this.rows[i] as UnityEngine.Object);
        }
    }

    protected override void OnSizeChanged() {
        base.OnSizeChanged();

        if (this.panel == null) return;

        this.panel.size = this.size;

        var uiScrollbar = this.scrollbar;
        if (uiScrollbar != null) {
            uiScrollbar.height = this.height;
            uiScrollbar.trackObject.height = this.height;
            uiScrollbar.AlignTo(this, UIAlignAnchor.TopRight);
        }

        this.CheckRows();
    }

    protected override void OnMouseWheel(UIMouseEventParameter p) {
        base.OnMouseWheel(p);

        if (this.mStepSize > 0 && this.mRowHeight > 0)
            this.ListPosition = this.mPos - p.wheelDelta * this.mStepSize / this.mRowHeight;
        else
            this.ListPosition = this.mPos - p.wheelDelta;

        if (this.SelectOnMouseEnter)
            this.OnRowClicked(this.mLastMouseEnter, p);
    }

    #endregion

    #region Private methods

    protected void OnRowClicked(UIComponent? component, UIMouseEventParameter p) {
        if (this.SelectOnMouseEnter) this.mLastMouseEnter = component;

        var rowsData = this.mRowsData;
        if (rowsData != null) {
            var fastList = this.rows;
            if (fastList != null) {
                var max = Mathf.Min(rowsData.m_size, fastList.m_size);
                for (var i = 0; i < max; i++) {
                    // ReSharper disable once SuspiciousTypeConversion.Global
                    if (component == (UIComponent) fastList[i]) {
                        this.SelectedIndex = i + Mathf.FloorToInt(this.mPos);
                        return;
                    }
                }
            }
        }
    }

    private void CheckRows() {
        if (this.panel == null || this.mRowHeight <= 0) return;

        var nbRows = Mathf.CeilToInt(this.height / this.mRowHeight) + 1;

        if (this.rows == null) {
            this.rows = new FastList<IUIFastListRow>();
            this.rows.SetCapacity(nbRows);
        }

        if (this.rows.m_size < nbRows) {
            // Adding missing rows
            for (var i = this.rows.m_size; i < nbRows; i++) {
                // ReSharper disable once SuspiciousTypeConversion.Global
                this.rows.Add((IUIFastListRow) this.panel.AddUIComponent(this.mRowType));
                switch (this.mCanSelect) {
                    case true when !this.SelectOnMouseEnter:
                        this.rows[i].eventClick += this.OnRowClicked;
                        break;
                    case true:
                        this.rows[i].eventMouseEnter += this.OnRowClicked;
                        break;
                }
            }
        }
        else if (this.rows.m_size > nbRows) {
            // Remove excess rows
            for (var i = nbRows; i < this.rows.m_size; i++)
                // ReSharper disable once SuspiciousTypeConversion.Global
                Destroy(this.rows[i] as UnityEngine.Object);

            this.rows.SetCapacity(nbRows);
        }

        this.UpdateScrollbar();
    }

    private void UpdateScrollbar() {
        if (this.mRowsData == null || this.mRowHeight <= 0) return;

        var uiScrollbar = this.scrollbar;

        if (this.mAutoHideScrollbar) {
            var visible = this.mRowsData.m_size * this.mRowHeight > this.height;
            var newPanelWidth = visible ? this.width - 10f : this.width;
            var newItemWidth = visible ? this.width - 20f : this.width;

            var uiPanel = this.panel;
            if (uiPanel != null) {
                uiPanel.width = newPanelWidth;
            }

            var fastList = this.rows;
            if (fastList != null) {
                for (var i = 0; i < fastList.m_size; i++) {
                    fastList[i].width = newItemWidth;
                }
            }

            if (uiScrollbar != null) {
                uiScrollbar.isVisible = visible;
            }
        }

        var scrollSize = this.height * this.height / (this.mRowHeight * this.mRowsData.m_size);
        var amount = this.StepSize * this.height / (this.mRowHeight * this.mRowsData.m_size);

        if (uiScrollbar != null) {
            uiScrollbar.scrollSize = Mathf.Max(10f, scrollSize);
            uiScrollbar.minValue = 0f;
            uiScrollbar.maxValue = this.height;
            uiScrollbar.incrementAmount = Mathf.Max(1f, amount);
        }

        this.UpdateScrollPosition();
    }

    private void UpdateScrollPosition() {
        if (this.mLock || this.mRowHeight <= 0) return;

        this.mLock = true;

        var uiScrollbar = this.scrollbar;
        if (uiScrollbar != null) {
            var rowsData = this.mRowsData;
            if (rowsData != null) {
                var pos = this.mPos * (this.height - uiScrollbar.scrollSize) /
                          (rowsData.m_size - this.height / this.mRowHeight);
                if (Math.Abs(pos - uiScrollbar.value) > double.Epsilon)
                    uiScrollbar.value = pos;
            }
        }

        this.mLock = false;
    }

    private void SetupControls() {
        if (this.panel != null) return;

        // Panel
        this.panel = this.AddUIComponent<UIPanel>();
        this.panel.width = this.width - 10f;
        this.panel.height = this.height;
        this.panel.backgroundSprite = this.mBackgroundSprite;
        this.panel.color = this.mColor;
        this.panel.clipChildren = true;
        this.panel.relativePosition = Vector2.zero;

        // Scrollbar
        this.scrollbar = this.AddUIComponent<UIScrollbar>();
        this.scrollbar.width = 20f;
        this.scrollbar.height = this.height;
        this.scrollbar.orientation = UIOrientation.Vertical;
        this.scrollbar.pivot = UIPivotPoint.BottomLeft;
        this.scrollbar.AlignTo(this, UIAlignAnchor.TopRight);
        this.scrollbar.minValue = 0;
        this.scrollbar.value = 0;
        this.scrollbar.incrementAmount = 50;

        var tracSprite = this.scrollbar.AddUIComponent<UISlicedSprite>();
        tracSprite.relativePosition = Vector2.zero;
        tracSprite.autoSize = true;
        tracSprite.size = tracSprite.parent.size;
        tracSprite.fillDirection = UIFillDirection.Vertical;
        tracSprite.spriteName = "ScrollbarTrack";

        this.scrollbar.trackObject = tracSprite;

        var thumbSprite = tracSprite.AddUIComponent<UISlicedSprite>();
        thumbSprite.relativePosition = Vector2.zero;
        thumbSprite.fillDirection = UIFillDirection.Vertical;
        thumbSprite.autoSize = true;
        thumbSprite.width = thumbSprite.parent.width - 8;
        thumbSprite.spriteName = "ScrollbarThumb";

        this.scrollbar.thumbObject = thumbSprite;

        // Rows
        this.CheckRows();

        this.scrollbar.eventValueChanged += (_, _) => {
            if (this.mLock || this.mRowHeight <= 0) return;

            this.mLock = true;

            var rowsData = this.mRowsData;
            if (rowsData != null) {
                this.ListPosition = this.scrollbar.value * (rowsData.m_size - this.height / this.mRowHeight) /
                                    (this.height - this.scrollbar.scrollSize - 1f);
            }

            this.mLock = false;
        };
    }

    #endregion
}
