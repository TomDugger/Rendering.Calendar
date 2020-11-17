using Rendering.Calendar.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Rendering.Calendar {
    public delegate void SelectedEventHandler(IEnumerable selectedRows, IEnumerable selectedColumns, IEnumerable selectedItems);

    public class MultiPresentationBox : ScrollableElement {

        ImageBrush ItemsBrush;
        RenderTargetBitmap ItemsBitmap = default(RenderTargetBitmap);
        ArrayList Items;
        bool needItemsRendering;
        ContentPresenter itemPresenter;
        ContentPresenter selectedItemPresenter;

        ImageBrush RowsBrush;
        RenderTargetBitmap RowsBitmap = default(RenderTargetBitmap);
        ArrayList Rows;
        bool needRowsRendering;
        ContentPresenter rowPresenter;
        ContentPresenter selectedRowPresenter;

        ImageBrush ColumnsBrush;
        RenderTargetBitmap ColumnsBitmap = default(RenderTargetBitmap);
        ArrayList Columns;
        bool needColumnsRendering;
        ContentPresenter columnPresenter;
        ContentPresenter selectedColumnPresenter;

        Dictionary<object, Position> EmptyPositions;

        ImageBrush SelectedItemsBrush;
        RenderTargetBitmap SelectedItemBitmap = default(RenderTargetBitmap);
        ArrayList SItems;
        bool needSelectedItemsRendering;

        ImageBrush SelectedRowsBrush;
        RenderTargetBitmap SelectedRowBitmap = default(RenderTargetBitmap);
        ArrayList SRows;
        bool needSelectedRowsRendering;
        int HorizontalSpanRowRendering = 1;

        ImageBrush SelectedColumnsBrush;
        RenderTargetBitmap SelectedColumnBitmap = default(RenderTargetBitmap);
        ArrayList SColumns;
        bool needSelectedColumnsRendering;
        int VerticalSpanColumnRendering = 1;


        ImageBrush MouseOverItemsBrush;
        RenderTargetBitmap MouseOverItemsBitmap = default(RenderTargetBitmap);

        ImageBrush MouseOverRowsBrush;
        RenderTargetBitmap MouseOverRowsBitmap = default(RenderTargetBitmap);

        ImageBrush MouseOverColumnsBrush;
        RenderTargetBitmap MouseOverColumnsBitmap = default(RenderTargetBitmap);

        ImageBrush HighlightBrush;
        RenderTargetBitmap HighlightBitmap = default(RenderTargetBitmap);

        object MouseOverElement;

        VisualBrush ItemsGridBrush;
        TranslateTransform gridTranslate = new TranslateTransform();
        TranslateTransform gridInverseTranslate = new TranslateTransform();

        private Point StartPointRange = default(Point);

        static MultiPresentationBox() {
            ItemSizeProperty.OverrideMetadata(typeof(MultiPresentationBox), new PropertyMetadata(new Size(100, 30), SimpleRefreshVisual));
        }

        public MultiPresentationBox() {

            Items = new ArrayList();
            itemPresenter = new ContentPresenter { ContentTemplate = ItemTemplate, ContentTemplateSelector = ItemTemplateSelector };
            selectedItemPresenter = new ContentPresenter { ContentTemplate = SelectedItemTemplate };

            Rows = new ArrayList();
            rowPresenter = new ContentPresenter { ContentTemplate = RowTemplate, ContentTemplateSelector = RowTemplateSelector };
            selectedRowPresenter = new ContentPresenter { ContentTemplate = SelectedRowTemplate };

            Columns = new ArrayList();
            columnPresenter = new ContentPresenter { ContentTemplate = ColumnTemplate, ContentTemplateSelector = ColumnsTemplateSelector };
            selectedColumnPresenter = new ContentPresenter { ContentTemplate = SelectedColumnTemplate };

            SItems = new ArrayList();
            SRows = new ArrayList();
            SColumns = new ArrayList();

            EmptyPositions = new Dictionary<object, Position>();

            SelectedItems = new ObservableCollection<KeyValuePair<object, Position>>();
            SelectedRows = new ObservableCollection<KeyValuePair<object, Position>>();
            SelectedColumns = new ObservableCollection<KeyValuePair<object, Position>>();

            CompositionTarget.Rendering += Rendering;

            ItemsGridBrush = new VisualBrush { Transform = gridInverseTranslate, Stretch = Stretch.None, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top, TileMode = TileMode.Tile, ViewportUnits = BrushMappingMode.Absolute, Viewport = new Rect(GetItemSize()) };
        }

        #region Events
        public SelectedEventHandler SelectedEventHandler;
        #endregion

        #region DepdendencyProperty

        #region Source
        public IEnumerable ItemsSource {
            get { return (IEnumerable)GetValue(ItemsSourceProperty); }
            set { SetValue(ItemsSourceProperty, value); }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(MultiPresentationBox), new PropertyMetadata(null, (s, a) => {
                var sender = (MultiPresentationBox)s;

                sender.SetItemPositions()
                .RefreshVisuals(sender.ItemsBitmap, sender.GetItemsRenderBounds(), sender.Items, sender.ItemsPositions, sender.GetItemSize(), ref sender.needItemsRendering, stopRender: true, filter: sender.ItemsFilter != null);
            }));



        public KeyValuePair<object, Position> LastSelectedItem {
            get { return (KeyValuePair<object, Position>)GetValue(LastSelectedItemProperty); }
            set { SetValue(LastSelectedItemProperty, value); }
        }

        public static readonly DependencyProperty LastSelectedItemProperty =
            DependencyProperty.Register("LastSelectedItem", typeof(KeyValuePair<object, Position>), typeof(MultiPresentationBox), new PropertyMetadata(default(KeyValuePair<object, Position>)));



        public IEnumerable RowsSource {
            get { return (IEnumerable)GetValue(RowsSourceProperty); }
            set { SetValue(RowsSourceProperty, value); }
        }

        public static readonly DependencyProperty RowsSourceProperty =
            DependencyProperty.Register("RowsSource", typeof(IEnumerable), typeof(MultiPresentationBox), new PropertyMetadata(null, (s, a) => {
                var sender = (MultiPresentationBox)s;

                sender.SetPositions(sender.RowsPositions, sender.RowsSource, sender.RowPositionFunc, sender.RowGroupsFunc, out sender.HorizontalSpanRowRendering)
                .SetOffsetPositions(sender.RowsPositions, 0, sender.HorizontalSpanRowRendering - 1)
                .SetScrollExtent()
                .RefreshVisuals(sender.RowsBitmap, sender.GetRowsRenderBounds(), sender.Rows, sender.RowsPositions, sender.GetRowSize(), ref sender.needRowsRendering, stopRender: true)

                .SetItemPositions()
                .RefreshVisuals(sender.ItemsBitmap, sender.GetItemsRenderBounds(), sender.Items, sender.ItemsPositions, sender.GetItemSize(), ref sender.needItemsRendering, stopRender: true, filter: sender.ItemsFilter != null);
            }));


        public IEnumerable ColumnsSource {
            get { return (IEnumerable)GetValue(ColumnsSourceProperty); }
            set { SetValue(ColumnsSourceProperty, value); }
        }

        public static readonly DependencyProperty ColumnsSourceProperty =
            DependencyProperty.Register("ColumnsSource", typeof(IEnumerable), typeof(MultiPresentationBox), new PropertyMetadata(null, (s, a) => {
                var sender = (MultiPresentationBox)s;

                sender.SetPositions(sender.ColumnsPositions, sender.ColumnsSource, sender.ColumnPositionFunc, sender.ColumnGroupsFunc, out sender.VerticalSpanColumnRendering)
                .SetOffsetPositions(sender.ColumnsPositions, sender.VerticalSpanColumnRendering - 1, 0)
                .SetScrollExtent()
                .RefreshVisuals(sender.ColumnsBitmap, sender.GetColumnRenderBounds(), sender.Columns, sender.ColumnsPositions, sender.GetColumnSize(), ref sender.needColumnsRendering, stopRender: true)

                .SetItemPositions()
                .RefreshVisuals(sender.ItemsBitmap, sender.GetItemsRenderBounds(), sender.Items, sender.ItemsPositions, sender.GetItemSize(), ref sender.needItemsRendering, stopRender: true, filter: sender.ItemsFilter != null);

            }));
        #endregion

        #region Positions
        public Dictionary<object, Position> ItemsPositions {
            get { return (Dictionary<object, Position>)GetValue(ItemsPositionsProperty); }
            set { SetValue(ItemsPositionsProperty, value); }
        }

        public static readonly DependencyProperty ItemsPositionsProperty =
            DependencyProperty.Register("ItemsPositions", typeof(Dictionary<object, Position>), typeof(MultiPresentationBox), new PropertyMetadata(GetDeaultNewDictionary()));



        public Dictionary<object, Position> RowsPositions {
            get { return (Dictionary<object, Position>)GetValue(RowsPositionsProperty); }
            set { SetValue(RowsPositionsProperty, value); }
        }

        public static readonly DependencyProperty RowsPositionsProperty =
            DependencyProperty.Register("RowsPositions", typeof(Dictionary<object, Position>), typeof(MultiPresentationBox), new PropertyMetadata(GetDeaultNewDictionary()));



        public Dictionary<object, Position> ColumnsPositions {
            get { return (Dictionary<object, Position>)GetValue(ColumnsPositionsProperty); }
            set { SetValue(ColumnsPositionsProperty, value); }
        }

        public static readonly DependencyProperty ColumnsPositionsProperty =
            DependencyProperty.Register("ColumnsPositions", typeof(Dictionary<object, Position>), typeof(MultiPresentationBox), new PropertyMetadata(GetDeaultNewDictionary()));


        #endregion


        #region Filter
        public Predicate<object> ItemsFilter {
            get { return (Predicate<object>)GetValue(ItemsFilterProperty); }
            set { SetValue(ItemsFilterProperty, value); }
        }

        public static readonly DependencyProperty ItemsFilterProperty =
            DependencyProperty.Register("ItemsFilter", typeof(Predicate<object>), typeof(MultiPresentationBox), new PropertyMetadata(null, (s, a) => {
                var sender = (MultiPresentationBox)s;

                sender.ClearAllSelecting(true).RefreshVisuals(sender.ItemsBitmap, sender.GetItemsRenderBounds(), sender.Items, sender.ItemsPositions, sender.GetItemSize(), ref sender.needItemsRendering, stopRender: true, filter: sender.ItemsFilter != null)
                .RefreshVisualByCollection(sender.SelectedItemBitmap, sender.SelectedItems, sender.SItems, sender.GetItemSize(), sender.GetItemsRenderBounds(), ref sender.needSelectedItemsRendering);
            }));
        #endregion


        #region Templates
        public DataTemplate ItemTemplate {
            get { return (DataTemplate)GetValue(ItemTemplateProperty); }
            set { SetValue(ItemTemplateProperty, value); }
        }

        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register("ItemTemplate", typeof(DataTemplate), typeof(MultiPresentationBox), new PropertyMetadata(default(DataTemplate), (s, a) => {
                if (s is MultiPresentationBox mpb)
                    mpb.itemPresenter = new ContentPresenter { ContentTemplate = mpb.ItemTemplate, ContentTemplateSelector = mpb.ItemTemplateSelector };
            }));


        public DataTemplateSelector ItemTemplateSelector {
            get { return (DataTemplateSelector)GetValue(ItemTemplateSelectorProperty); }
            set { SetValue(ItemTemplateSelectorProperty, value); }
        }

        public static readonly DependencyProperty ItemTemplateSelectorProperty =
            DependencyProperty.Register("ItemTemplateSelector", typeof(DataTemplateSelector), typeof(MultiPresentationBox), new PropertyMetadata(default(DataTemplateSelector), (s, a) => {
                if (s is MultiPresentationBox mpb)
                    mpb.itemPresenter = new ContentPresenter { ContentTemplate = mpb.ItemTemplate, ContentTemplateSelector = mpb.ItemTemplateSelector };
            }));



        public DataTemplate RowTemplate {
            get { return (DataTemplate)GetValue(RowTemplateProperty); }
            set { SetValue(RowTemplateProperty, value); }
        }

        public static readonly DependencyProperty RowTemplateProperty =
            DependencyProperty.Register("RowTemplate", typeof(DataTemplate), typeof(MultiPresentationBox), new PropertyMetadata(default(DataTemplate), (s, a) => {
                if (s is MultiPresentationBox mpb)
                    mpb.rowPresenter = new ContentPresenter { ContentTemplate = mpb.RowTemplate, ContentTemplateSelector = mpb.RowTemplateSelector };
            }));



        public DataTemplateSelector RowTemplateSelector {
            get { return (DataTemplateSelector)GetValue(RowTemplateSelectorProperty); }
            set { SetValue(RowTemplateSelectorProperty, value); }
        }

        public static readonly DependencyProperty RowTemplateSelectorProperty =
            DependencyProperty.Register("RowTemplateSelector", typeof(DataTemplateSelector), typeof(MultiPresentationBox), new PropertyMetadata(default(DataTemplateSelector), (s, a) => {
                if (s is MultiPresentationBox mpb)
                    mpb.rowPresenter = new ContentPresenter { ContentTemplate = mpb.RowTemplate, ContentTemplateSelector = mpb.RowTemplateSelector };
            }));



        public DataTemplate ColumnTemplate {
            get { return (DataTemplate)GetValue(ColumnTemplateProperty); }
            set { SetValue(ColumnTemplateProperty, value); }
        }

        public static readonly DependencyProperty ColumnTemplateProperty =
            DependencyProperty.Register("ColumnTemplate", typeof(DataTemplate), typeof(MultiPresentationBox), new PropertyMetadata(default(DataTemplate), (s, a) => {
                if (s is MultiPresentationBox mpb)
                    mpb.columnPresenter = new ContentPresenter { ContentTemplate = mpb.ColumnTemplate, ContentTemplateSelector = mpb.ColumnsTemplateSelector };
            }));



        public DataTemplateSelector ColumnsTemplateSelector {
            get { return (DataTemplateSelector)GetValue(ColumnsTemplateSelectorProperty); }
            set { SetValue(ColumnsTemplateSelectorProperty, value); }
        }

        public static readonly DependencyProperty ColumnsTemplateSelectorProperty =
            DependencyProperty.Register("ColumnsTemplateSelector", typeof(DataTemplateSelector), typeof(MultiPresentationBox), new PropertyMetadata(default(DataTemplateSelector), (s, a) => {
                if (s is MultiPresentationBox mpb)
                    mpb.columnPresenter = new ContentPresenter { ContentTemplate = mpb.ColumnTemplate, ContentTemplateSelector = mpb.ColumnsTemplateSelector };
            }));
        #endregion


        public double RowWidth {
            get { return (double)GetValue(RowWidthProperty); }
            set { SetValue(RowWidthProperty, value); }
        }

        public static readonly DependencyProperty RowWidthProperty =
            DependencyProperty.Register("RowWidth", typeof(double), typeof(MultiPresentationBox), new PropertyMetadata(100.0));


        public double ColumnsHeight {
            get { return (double)GetValue(ColumnsHeightProperty); }
            set { SetValue(ColumnsHeightProperty, value); }
        }

        public static readonly DependencyProperty ColumnsHeightProperty =
            DependencyProperty.Register("ColumnsHeight", typeof(double), typeof(MultiPresentationBox), new PropertyMetadata(30.0));



        #region MouseOver
        public SolidColorBrush MouseOverColor {
            get { return (SolidColorBrush)GetValue(MouseOverColorProperty); }
            set { SetValue(MouseOverColorProperty, value); }
        }

        public static readonly DependencyProperty MouseOverColorProperty =
            DependencyProperty.Register("MouseOverColor", typeof(SolidColorBrush), typeof(MultiPresentationBox), new PropertyMetadata(null));


        public Thickness MouseOverThickness {
            get { return (Thickness)GetValue(MouseOverThicknessProperty); }
            set { SetValue(MouseOverThicknessProperty, value); }
        }

        public static readonly DependencyProperty MouseOverThicknessProperty =
            DependencyProperty.Register("MouseOverThickness", typeof(Thickness), typeof(MultiPresentationBox), new PropertyMetadata(default(Thickness)));
        #endregion


        #region Funcs
        public Func<Dictionary<object, Position>, Dictionary<object, Position>, object, Position?> ItemPositionFunc {
            get { return (Func<Dictionary<object, Position>, Dictionary<object, Position>, object, Position?>)GetValue(ItemPositionFuncProperty); }
            set { SetValue(ItemPositionFuncProperty, value); }
        }

        public static readonly DependencyProperty ItemPositionFuncProperty =
            DependencyProperty.Register("ItemPositionFunc", typeof(Func<Dictionary<object, Position>, Dictionary<object, Position>, object, Position?>), typeof(MultiPresentationBox), new PropertyMetadata(null, (s, a) => {
                var sender = (MultiPresentationBox)s;

                sender.SetItemPositions()
                .RefreshVisuals(sender.ItemsBitmap, sender.GetItemsRenderBounds(), sender.Items, sender.ItemsPositions, sender.GetItemSize(), ref sender.needItemsRendering, stopRender: true, filter: sender.ItemsFilter != null);
            }));



        public Func<int[,], Dictionary<object, int>, Dictionary<object, int>, object, Position, Position?> ItemPlacingFunc {
            get { return (Func<int[,], Dictionary<object, int>, Dictionary<object, int>, object, Position, Position?>)GetValue(ItemPlacingFuncProperty); }
            set { SetValue(ItemPlacingFuncProperty, value); }
        }

        public static readonly DependencyProperty ItemPlacingFuncProperty =
            DependencyProperty.Register("ItemPlacingFunc", typeof(Func<int[,], Dictionary<object, int>, Dictionary<object, int>, object, Position, Position?>), typeof(MultiPresentationBox), new PropertyMetadata(null));




        public Func<IEnumerable, object, Position, object, Position> RowPositionFunc {
            get { return (Func<IEnumerable, object, Position, object, Position>)GetValue(RowPositionFuncProperty); }
            set { SetValue(RowPositionFuncProperty, value); }
        }

        public static readonly DependencyProperty RowPositionFuncProperty =
            DependencyProperty.Register("RowPositionFunc", typeof(Func<IEnumerable, object, Position, object, Position>), typeof(MultiPresentationBox), new PropertyMetadata(null, (s, a) => {
                var sender = (MultiPresentationBox)s;

                sender.SetPositions(sender.RowsPositions, sender.RowsSource, sender.RowPositionFunc, sender.RowGroupsFunc, out sender.HorizontalSpanRowRendering)
                .SetOffsetPositions(sender.RowsPositions, 0, sender.HorizontalSpanRowRendering - 1)
                .RefreshVisuals(sender.RowsBitmap, sender.GetRowsRenderBounds(), sender.Rows, sender.RowsPositions, sender.GetRowSize(), ref sender.needRowsRendering, stopRender: true)

                .SetScrollExtent()

                .SetItemPositions()
                .RefreshVisuals(sender.ItemsBitmap, sender.GetItemsRenderBounds(), sender.Items, sender.ItemsPositions, sender.GetItemSize(), ref sender.needItemsRendering, stopRender: true, filter: sender.ItemsFilter != null);
            }));


        public Func<Dictionary<object, Position>, int> RowGroupsFunc {
            get { return (Func<Dictionary<object, Position>, int>)GetValue(RowGroupsFuncProperty); }
            set { SetValue(RowGroupsFuncProperty, value); }
        }

        public static readonly DependencyProperty RowGroupsFuncProperty =
            DependencyProperty.Register("RowGroupsFunc", typeof(Func<Dictionary<object, Position>, int>), typeof(MultiPresentationBox), new PropertyMetadata(null, (s, a) => {
                var sender = (MultiPresentationBox)s;

                sender.SetPositions(sender.RowsPositions, sender.RowsSource, sender.RowPositionFunc, sender.RowGroupsFunc, out sender.HorizontalSpanRowRendering)
                .SetOffsetPositions(sender.RowsPositions, 0, sender.HorizontalSpanRowRendering - 1)
                .RebuildRenderMap(sender.RenderSize)
                .RefreshVisuals(sender.RowsBitmap, sender.GetRowsRenderBounds(), sender.Rows, sender.RowsPositions, sender.GetRowSize(), ref sender.needRowsRendering, stopRender: true)
                .SetScrollExtent()

                .SetItemPositions()
                .RefreshVisuals(sender.ItemsBitmap, sender.GetItemsRenderBounds(), sender.Items, sender.ItemsPositions, sender.GetItemSize(), ref sender.needItemsRendering, stopRender: true, filter: sender.ItemsFilter != null);
            }));



        public Func<IEnumerable, object, Position, object, Position> ColumnPositionFunc {
            get { return (Func<IEnumerable, object, Position, object, Position>)GetValue(ColumnPositionFuncProperty); }
            set { SetValue(ColumnPositionFuncProperty, value); }
        }

        public static readonly DependencyProperty ColumnPositionFuncProperty =
            DependencyProperty.Register("ColumnPositionFunc", typeof(Func<IEnumerable, object, Position, object, Position>), typeof(MultiPresentationBox), new PropertyMetadata(null, (s, a) => {
                var sender = (MultiPresentationBox)s;

                sender.SetPositions(sender.ColumnsPositions, sender.ColumnsSource, sender.ColumnPositionFunc, sender.ColumnGroupsFunc, out sender.VerticalSpanColumnRendering)
                .SetOffsetPositions(sender.ColumnsPositions, sender.VerticalSpanColumnRendering - 1, 0)
                .RebuildRenderMap(sender.RenderSize)
                .RefreshVisuals(sender.ColumnsBitmap, sender.GetColumnRenderBounds(), sender.Columns, sender.ColumnsPositions, sender.GetColumnSize(), ref sender.needColumnsRendering, stopRender: true)

                .SetScrollExtent()

                .SetItemPositions()
                .RefreshVisuals(sender.ItemsBitmap, sender.GetItemsRenderBounds(), sender.Items, sender.ItemsPositions, sender.GetItemSize(), ref sender.needItemsRendering, stopRender: true, filter: sender.ItemsFilter != null);
            }));


        public Func<Dictionary<object, Position>, int> ColumnGroupsFunc {
            get { return (Func<Dictionary<object, Position>, int>)GetValue(ColumnGroupsFuncProperty); }
            set { SetValue(ColumnGroupsFuncProperty, value); }
        }

        public static readonly DependencyProperty ColumnGroupsFuncProperty =
            DependencyProperty.Register("ColumnGroupsFunc", typeof(Func<Dictionary<object, Position>, int>), typeof(MultiPresentationBox), new PropertyMetadata(null, (s, a) => {
                var sender = (MultiPresentationBox)s;

                sender.SetPositions(sender.ColumnsPositions, sender.ColumnsSource, sender.ColumnPositionFunc, sender.ColumnGroupsFunc, out sender.VerticalSpanColumnRendering)
                .SetOffsetPositions(sender.ColumnsPositions, sender.VerticalSpanColumnRendering - 1, 0)
                .RebuildRenderMap(sender.RenderSize)
                .RefreshVisuals(sender.ColumnsBitmap, sender.GetColumnRenderBounds(), sender.Columns, sender.ColumnsPositions, sender.GetColumnSize(), ref sender.needColumnsRendering, stopRender: true)

                .SetScrollExtent()

                .SetItemPositions()
                .RefreshVisuals(sender.ItemsBitmap, sender.GetItemsRenderBounds(), sender.Items, sender.ItemsPositions, sender.GetItemSize(), ref sender.needItemsRendering, stopRender: true, filter: sender.ItemsFilter != null);
            }));

        #endregion


        #region Rendering
        public int ItemRenderQuantity {
            get { return (int)GetValue(ItemRenderQuantityProperty); }
            set { SetValue(ItemRenderQuantityProperty, value); }
        }

        public static readonly DependencyProperty ItemRenderQuantityProperty =
            DependencyProperty.Register("ItemRenderQuantity", typeof(int), typeof(MultiPresentationBox), new PropertyMetadata(50));


        public int RowRenderQuantity {
            get { return (int)GetValue(RowRenderQuantityProperty); }
            set { SetValue(RowRenderQuantityProperty, value); }
        }

        public static readonly DependencyProperty RowRenderQuantityProperty =
            DependencyProperty.Register("RowRenderQuantity", typeof(int), typeof(MultiPresentationBox), new PropertyMetadata(10));


        public int ColumnRenderQuantity {
            get { return (int)GetValue(ColumnRenderQuantityProperty); }
            set { SetValue(ColumnRenderQuantityProperty, value); }
        }

        public static readonly DependencyProperty ColumnRenderQuantityProperty =
            DependencyProperty.Register("ColumnRenderQuantity", typeof(int), typeof(MultiPresentationBox), new PropertyMetadata(10));


        public int SelectRenderQuantity {
            get { return (int)GetValue(SelectRenderQuantityProperty); }
            set { SetValue(SelectRenderQuantityProperty, value); }
        }

        public static readonly DependencyProperty SelectRenderQuantityProperty =
            DependencyProperty.Register("SelectRenderQuantity", typeof(int), typeof(MultiPresentationBox), new PropertyMetadata(1000));
        #endregion


        #region Selecting
        public bool SelectingRowWithItems {
            get { return (bool)GetValue(SelectingRowWithItemsProperty); }
            set { SetValue(SelectingRowWithItemsProperty, value); }
        }

        public static readonly DependencyProperty SelectingRowWithItemsProperty =
            DependencyProperty.Register("SelectingRowWithItems", typeof(bool), typeof(MultiPresentationBox), new PropertyMetadata(false));


        public bool SelectingColumnWithItems {
            get { return (bool)GetValue(SelectingColumnWithItemsProperty); }
            set { SetValue(SelectingColumnWithItemsProperty, value); }
        }

        public static readonly DependencyProperty SelectingColumnWithItemsProperty =
            DependencyProperty.Register("SelectingColumnWithItems", typeof(bool), typeof(MultiPresentationBox), new PropertyMetadata(false));


        public bool SelectingItemWithRowsAndColumns {
            get { return (bool)GetValue(SelectingItemWithRowsAndColumnsProperty); }
            set { SetValue(SelectingItemWithRowsAndColumnsProperty, value); }
        }

        public static readonly DependencyProperty SelectingItemWithRowsAndColumnsProperty =
            DependencyProperty.Register("SelectingItemWithRowsAndColumns", typeof(bool), typeof(MultiPresentationBox), new PropertyMetadata(false));


        public bool AllowSelectingEmptyItem {
            get { return (bool)GetValue(AllowSelectingEmptyItemProperty); }
            set { SetValue(AllowSelectingEmptyItemProperty, value); }
        }

        public static readonly DependencyProperty AllowSelectingEmptyItemProperty =
            DependencyProperty.Register("AllowSelectingEmptyItem", typeof(bool), typeof(MultiPresentationBox), new PropertyMetadata(false));


        public bool MultySelecting {
            get { return (bool)GetValue(MultySelectingProperty); }
            set { SetValue(MultySelectingProperty, value); }
        }

        public static readonly DependencyProperty MultySelectingProperty =
            DependencyProperty.Register("MultySelecting", typeof(bool), typeof(MultiPresentationBox), new PropertyMetadata(false));



        public bool AllowToHighlightRangeElements {
            get { return (bool)GetValue(AllowToHighlightRangeElementsProperty); }
            set { SetValue(AllowToHighlightRangeElementsProperty, value); }
        }

        public static readonly DependencyProperty AllowToHighlightRangeElementsProperty =
            DependencyProperty.Register("AllowToHighlightRangeElements", typeof(bool), typeof(MultiPresentationBox), new PropertyMetadata(false));



        public ObservableCollection<KeyValuePair<object, Position>> SelectedItems {
            get { return (ObservableCollection<KeyValuePair<object, Position>>)GetValue(SelectedItemsProperty); }
            set { SetValue(SelectedItemsProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.Register("SelectedItems", typeof(ObservableCollection<KeyValuePair<object, Position>>), typeof(MultiPresentationBox), new PropertyMetadata(null));


        public ObservableCollection<KeyValuePair<object, Position>> SelectedRows {
            get { return (ObservableCollection<KeyValuePair<object, Position>>)GetValue(SelectedRowsProperty); }
            set { SetValue(SelectedRowsProperty, value); }
        }

        public static readonly DependencyProperty SelectedRowsProperty =
            DependencyProperty.Register("SelectedRows", typeof(ObservableCollection<KeyValuePair<object, Position>>), typeof(MultiPresentationBox), new PropertyMetadata(null));



        public ObservableCollection<KeyValuePair<object, Position>> SelectedColumns {
            get { return (ObservableCollection<KeyValuePair<object, Position>>)GetValue(SelectedColumnsProperty); }
            set { SetValue(SelectedColumnsProperty, value); }
        }

        public static readonly DependencyProperty SelectedColumnsProperty =
            DependencyProperty.Register("SelectedColumns", typeof(ObservableCollection<KeyValuePair<object, Position>>), typeof(MultiPresentationBox), new PropertyMetadata(null));



        public DataTemplate SelectedItemTemplate {
            get { return (DataTemplate)GetValue(SelectedItemTemplateProperty); }
            set { SetValue(SelectedItemTemplateProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemTemplateProperty =
            DependencyProperty.Register("SelectedItemTemplate", typeof(DataTemplate), typeof(MultiPresentationBox), new PropertyMetadata(null, (s, a) => {
                var sender = (s as MultiPresentationBox);

                sender.selectedItemPresenter = new ContentPresenter { ContentTemplate = sender.SelectedItemTemplate };

                sender.RefreshVisualByCollection(sender.SelectedItemBitmap, sender.SelectedItems, sender.SItems, sender.GetItemSize(), sender.GetItemsRenderBounds(), ref sender.needSelectedItemsRendering);
            }));


        public DataTemplate SelectedRowTemplate {
            get { return (DataTemplate)GetValue(SelectedRowTemplateProperty); }
            set { SetValue(SelectedRowTemplateProperty, value); }
        }

        public static readonly DependencyProperty SelectedRowTemplateProperty =
            DependencyProperty.Register("SelectedRowTemplate", typeof(DataTemplate), typeof(MultiPresentationBox), new PropertyMetadata(null, (s, a) => {
                var sender = (s as MultiPresentationBox);

                sender.selectedRowPresenter = new ContentPresenter { ContentTemplate = sender.SelectedRowTemplate };

                sender.RefreshVisualByCollection(sender.SelectedRowBitmap, sender.SelectedRows, sender.SRows, sender.GetRowSize(), sender.GetRowsRenderBounds(), ref sender.needSelectedRowsRendering);
            }));


        public DataTemplate SelectedColumnTemplate {
            get { return (DataTemplate)GetValue(SelectedColumnTemplateProperty); }
            set { SetValue(SelectedColumnTemplateProperty, value); }
        }

        public static readonly DependencyProperty SelectedColumnTemplateProperty =
            DependencyProperty.Register("SelectedColumnTemplate", typeof(DataTemplate), typeof(MultiPresentationBox), new PropertyMetadata(null, (s, a) => {
                var sender = (s as MultiPresentationBox);

                sender.selectedColumnPresenter = new ContentPresenter { ContentTemplate = sender.SelectedColumnTemplate };

                sender.RefreshVisualByCollection(sender.SelectedColumnBitmap, sender.SelectedColumns, sender.SColumns, sender.GetColumnSize(), sender.GetColumnRenderBounds(), ref sender.needSelectedColumnsRendering);
            }));
        #endregion


        #region Grid
        public Brush GridLineBrush {
            get { return (Brush)GetValue(GridLineBrushProperty); }
            set { SetValue(GridLineBrushProperty, value); }
        }

        public static readonly DependencyProperty GridLineBrushProperty =
            DependencyProperty.Register("GridLineBrush", typeof(Brush), typeof(MultiPresentationBox), new PropertyMetadata(null, (s, a) => {
                var sender = (MultiPresentationBox)s;

                if (sender.ShowItemsGrid)
                    sender.RefreshGridBrush(sender.ItemsGridBrush, sender.GetItemSize(), sender.GridLineBrush, sender.GridLineThickness);
            }));


        public Thickness GridLineThickness {
            get { return (Thickness)GetValue(GridLineThicknessProperty); }
            set { SetValue(GridLineThicknessProperty, value); }
        }

        public static readonly DependencyProperty GridLineThicknessProperty =
            DependencyProperty.Register("GridLineThickness", typeof(Thickness), typeof(MultiPresentationBox), new PropertyMetadata(default(Thickness), (s, a) => {
                var sender = (MultiPresentationBox)s;

                if (sender.ShowItemsGrid)
                    sender.RefreshGridBrush(sender.ItemsGridBrush, sender.GetItemSize(), sender.GridLineBrush, sender.GridLineThickness);
            }));



        public bool ShowItemsGrid {
            get { return (bool)GetValue(ShowItemsGridProperty); }
            set { SetValue(ShowItemsGridProperty, value); }
        }

        public static readonly DependencyProperty ShowItemsGridProperty =
            DependencyProperty.Register("ShowItemsGrid", typeof(bool), typeof(MultiPresentationBox), new PropertyMetadata(false, (s, a) => {
                var sender = (MultiPresentationBox)s;
                if ((bool)a.NewValue)
                    sender.RefreshGridBrush(sender.ItemsGridBrush, sender.GetItemSize(), sender.GridLineBrush, sender.GridLineThickness);
                else
                    sender.ItemsGridBrush.Visual = null;
            }));
        #endregion
        #endregion

        #region DependencyPropertyChanged
        private static void SimpleRefreshVisual(DependencyObject s, DependencyPropertyChangedEventArgs a) {
            var sender = (MultiPresentationBox)s;

            sender.SetScrollExtent()
                .RefreshVisuals(sender.RowsBitmap, sender.GetRowsRenderBounds(), sender.Rows, sender.RowsPositions, sender.GetRowSize(), ref sender.needRowsRendering, stopRender: true)
                .RefreshVisuals(sender.ColumnsBitmap, sender.GetColumnRenderBounds(), sender.Columns, sender.ColumnsPositions, sender.GetColumnSize(), ref sender.needColumnsRendering, stopRender: true)
                .RefreshVisuals(sender.ItemsBitmap, sender.GetItemsRenderBounds(), sender.Items, sender.ItemsPositions, sender.GetItemSize(), ref sender.needItemsRendering, stopRender: true, filter: sender.ItemsFilter != null)

                .RefreshVisualByCollection(sender.SelectedItemBitmap, sender.SelectedItems, sender.SItems, sender.GetItemSize(), sender.GetItemsRenderBounds(), ref sender.needSelectedItemsRendering)
                .RefreshVisualByCollection(sender.SelectedRowBitmap, sender.SelectedRows, sender.SRows, sender.GetRowSize(), sender.GetRowsRenderBounds(), ref sender.needSelectedRowsRendering)
                .RefreshVisualByCollection(sender.SelectedColumnBitmap, sender.SelectedColumns, sender.SColumns, sender.GetColumnSize(), sender.GetColumnRenderBounds(), ref sender.needSelectedColumnsRendering)

                .RefreshGridBrush(sender.ItemsGridBrush, sender.GetItemSize(), sender.GridLineBrush, sender.GridLineThickness);
        }
        #endregion

        #region Overrides
        protected override void OnRender(DrawingContext drawingContext) {

            int rowWidth = GetRowWidth();
            int columnHeight = GetColumnHeight();

            if (ItemsBitmap == null || RowsBitmap == null || ColumnsBitmap == null) return;


            var itemsRenderBounds = new Rect(rowWidth, columnHeight, ItemsBitmap.Width, ItemsBitmap.Height);
            var rowsRenderBounds = new Rect(0, columnHeight, RowsBitmap.Width, RowsBitmap.Height);
            var columnsRenderBounds = new Rect(rowWidth, 0, ColumnsBitmap.Width, ColumnsBitmap.Height);

            // grid
            drawingContext.PushTransform(new TranslateTransform(rowWidth, columnHeight));
            drawingContext.DrawRectangle(ItemsGridBrush, new Pen(), new Rect(0, 0, ItemsBitmap.Width, ItemsBitmap.Height));
            drawingContext.PushTransform(new TranslateTransform(-rowWidth, -columnHeight));

            // elements drawing
            if (ItemsBitmap != null)
                drawingContext.DrawRectangle(ItemsBrush, new Pen(), itemsRenderBounds);

            if (RowsBitmap != null)
                drawingContext.DrawRectangle(RowsBrush, new Pen(), rowsRenderBounds);

            if (ColumnsBitmap != null)
                drawingContext.DrawRectangle(ColumnsBrush, new Pen(), columnsRenderBounds);


            // selected drawing
            drawingContext.DrawRectangle(SelectedItemsBrush, new Pen(), itemsRenderBounds);

            drawingContext.DrawRectangle(SelectedRowsBrush, new Pen(), rowsRenderBounds);

            drawingContext.DrawRectangle(SelectedColumnsBrush, new Pen(), columnsRenderBounds);

            // mouse over items drawing
            drawingContext.DrawRectangle(MouseOverItemsBrush, new Pen(), itemsRenderBounds);

            // mouse over rows drawing
            drawingContext.DrawRectangle(MouseOverRowsBrush, new Pen(), rowsRenderBounds);

            // mouse over columns drawing
            drawingContext.DrawRectangle(MouseOverColumnsBrush, new Pen(), columnsRenderBounds);

            // hightlight rectangle
            drawingContext.DrawRectangle(HighlightBrush, new Pen(), new Rect(RenderSize));
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {

            RebuildRenderMap(sizeInfo.NewSize).SetScrollExtent();

            RefreshVisuals(ItemsBitmap, GetItemsRenderBounds(), Items, ItemsPositions, GetItemSize(), ref needItemsRendering, stopRender: true, filter: ItemsFilter != null)
            .RefreshVisuals(RowsBitmap, GetRowsRenderBounds(), Rows, RowsPositions, GetRowSize(), ref needRowsRendering, stopRender: true)
            .RefreshVisuals(ColumnsBitmap, GetColumnRenderBounds(), Columns, ColumnsPositions, GetColumnSize(), ref needColumnsRendering, stopRender: true)

            .RefreshVisualByCollection(SelectedItemBitmap, SelectedItems, SItems, GetItemSize(), GetItemsRenderBounds(), ref needSelectedItemsRendering)
            .RefreshVisualByCollection(SelectedRowBitmap, SelectedRows, SRows, GetRowSize(), GetRowsRenderBounds(), ref needSelectedRowsRendering)
            .RefreshVisualByCollection(SelectedColumnBitmap, SelectedColumns, SColumns, GetColumnSize(), GetColumnRenderBounds(), ref needSelectedColumnsRendering);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e) {
            base.OnMouseDown(e);

            Point point = e.GetPosition(this);

            int rowWidth = GetRowWidth();
            int columnHeight = GetColumnHeight();

            if (AllowToHighlightRangeElements && e.LeftButton == MouseButtonState.Pressed && point.X > rowWidth && point.Y > columnHeight)
                StartPointRange = point;
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            base.OnMouseMove(e);

            int rowWidth = GetRowWidth();
            int columnHeight = GetColumnHeight();

            Point position = e.GetPosition(this);

            if (e.LeftButton == MouseButtonState.Pressed && AllowToHighlightRangeElements && StartPointRange != default(Point)) {
                Rect highlightRect = new Rect(StartPointRange, position);

                ClearHightlightBitmap();

                HighlightBitmap?.Render(GetBorderVisual(highlightRect, MouseOverColor, MouseOverThickness));
            }
            // find into rows
            else if (position.X < rowWidth && position.Y > columnHeight) {

                position = new Point(position.X, position.Y - columnHeight + VerticalOffset);
                Size size = GetRowSize();

                foreach (var row in RowsPositions) {
                    Rect rect = GetRect(row.Value, size, false, false);

                    if (rect.Contains(position)) {

                        if (MouseOverElement != row.Key) {
                            MouseOverElement = row.Key;

                            Rect originalRect = GetRect(row.Value, size, false, true);
                            ClearMouseOverBitmap();

                            MouseOverRowsBitmap?.Render(GetBorderVisual(originalRect, MouseOverColor, MouseOverThickness));
                        }

                        break;
                    }
                }
            }

            // find into columns
            else if (position.X > rowWidth && position.Y < columnHeight) {
                position = new Point(position.X - rowWidth + HorizontalOffset, position.Y);
                Size size = GetColumnSize();

                foreach (var column in ColumnsPositions) {
                    Rect rect = GetRect(column.Value, size, false, false);

                    if (rect.Contains(position)) {

                        if (MouseOverElement != column.Key) {
                            MouseOverElement = column.Key;

                            Rect originalRect = GetRect(column.Value, size, true, false);
                            ClearMouseOverBitmap();

                            MouseOverColumnsBitmap?.Render(GetBorderVisual(originalRect, MouseOverColor, MouseOverThickness));
                        }

                        break;
                    }
                }
            }

            // find into items
            else if (position.X > rowWidth && position.Y > columnHeight) {
                position = new Point(position.X - rowWidth + HorizontalOffset, position.Y - columnHeight + VerticalOffset);

                Size size = GetItemSize();

                var item = ItemsPositions.FirstOrDefault(x => GetRect(x.Value, size, false, false).Contains(position));

                if (item.Key != null && ItemsFilter?.Invoke(item.Key) != false) {

                    if (MouseOverElement != item.Key) {
                        MouseOverElement = item.Key;

                        Rect originalRect = GetRect(item.Value, size, true, true);
                        ClearMouseOverBitmap();

                        MouseOverItemsBitmap?.Render(GetBorderVisual(originalRect, MouseOverColor, MouseOverThickness));
                    }
                }
                else {
                    MouseOverElement = null;

                    Rect originalRect = new Rect(new Point(
                        ((int)((position.X - HorizontalOffset) / size.Width)) * size.Width - HorizontalOffset % size.Width, 
                        ((int)((position.Y - VerticalOffset) / size.Height)) * size.Height - VerticalOffset % size.Height), 
                        size);
                    ClearMouseOverBitmap();

                    MouseOverItemsBitmap?.Render(GetBorderVisual(originalRect, MouseOverColor, MouseOverThickness));
                }
            }
        }

        protected override void OnMouseUp(MouseButtonEventArgs e) {
            base.OnMouseMove(e);

            if (e.ChangedButton == MouseButton.Left) {

                LastSelectedItem = default(KeyValuePair<object, Position>);

                int rowWidth = GetRowWidth();
                int columnHeight = GetColumnHeight();

                if (!MultySelecting) {
                    SelectedItems.Clear();
                    SelectedRows.Clear();
                    SelectedColumns.Clear();

                    EmptyPositions.Clear();

                    RefreshVisualByCollection(SelectedItemBitmap, SelectedItems, SItems, GetItemSize(), GetItemsRenderBounds(), ref needSelectedItemsRendering);
                    RefreshVisualByCollection(SelectedRowBitmap, SelectedRows, SRows, GetRowSize(), GetRowsRenderBounds(), ref needSelectedRowsRendering);
                    RefreshVisualByCollection(SelectedColumnBitmap, SelectedColumns, SColumns, GetColumnSize(), GetColumnRenderBounds(), ref needSelectedColumnsRendering);
                }

                Point position = e.GetPosition(this);

                if (AllowToHighlightRangeElements && StartPointRange != default(Point)) {

                    ClearHightlightBitmap();

                    Rect highlightRect = new Rect(StartPointRange, position);
                    highlightRect.Offset(-rowWidth + HorizontalOffset, -columnHeight + VerticalOffset);

                    // find element into the range
                    var selectedItems = ItemsPositions.Where(x => ItemsFilter?.Invoke(x.Key) != false && GetRect(x.Value, GetItemSize(), false, false).IntersectsWith(highlightRect));

                    SelectedItems.Clear();
                    EmptyPositions.Clear();

                    if (selectedItems.Any()) {

                        foreach (var select in selectedItems)
                            SelectedItems.Add(select);
                    }
                    else if (AllowSelectingEmptyItem) {
                        // creating one big empty element
                        Size size = GetItemSize();

                        var key = Guid.NewGuid();

                        Position originalPosition = new Position(highlightRect.Y / size.Height, highlightRect.X / size.Width, 1 + highlightRect.Height / size.Height, 1 + highlightRect.Width / size.Width);

                        SelectedItems.Add(new KeyValuePair<object, Position>(key, originalPosition));

                        EmptyPositions[key] = originalPosition;
                    }

                    LastSelectedItem = SelectedItems.LastOrDefault();

                    SelectedEventHandler?.Invoke(SelectedRows, SelectedColumns, SelectedColumns);

                    RefreshVisualByCollection(SelectedItemBitmap, SelectedItems, SItems, GetItemSize(), GetItemsRenderBounds(), ref needSelectedItemsRendering);

                    SelectedEventHandler?.Invoke(SelectedRows, SelectedColumns, SelectedColumns);

                    StartPointRange = default(Point);
                    ClearMouseOverBitmap();
                }
                // find into rows
                else if (position.X < rowWidth && position.Y > columnHeight) {

                    position = new Point(position.X, position.Y - columnHeight + VerticalOffset);
                    Size size = GetRowSize();

                    foreach (var row in RowsPositions) {
                        Rect rect = GetRect(row.Value, size, false, false);

                        if (rect.Contains(position)) {

                            if (SelectedRows.Contains(row)) {
                                SelectedRows.Remove(row);

                                if (SelectingRowWithItems) {
                                    foreach (var item in SelectedItems.Where(x => row.Value.IsValidateRow(x.Value.Row)).ToArray())
                                        SelectedItems.Remove(item);

                                    RefreshVisualByCollection(SelectedItemBitmap, SelectedItems, SItems, GetItemSize(), GetItemsRenderBounds(), ref needSelectedItemsRendering);
                                }
                            }
                            else {
                                SelectedRows.Add(row);

                                if (SelectingRowWithItems) {
                                    foreach (var item in ItemsPositions.Where(x => ItemsFilter?.Invoke(x.Key) != false && row.Value.IsValidateRowSpan(x.Value.Row, x.Value.FullRow)))
                                        SelectedItems.Add(item);

                                    RefreshVisualByCollection(SelectedItemBitmap, SelectedItems, SItems, GetItemSize(), GetItemsRenderBounds(), ref needSelectedItemsRendering);
                                }
                            }

                            RefreshVisualByCollection(SelectedRowBitmap, SelectedRows, SRows, GetRowSize(), GetRowsRenderBounds(), ref needSelectedRowsRendering);

                            break;
                        }
                    }

                    SelectedEventHandler?.Invoke(SelectedRows, SelectedColumns, SelectedColumns);
                }

                // find into columns
                else if (position.X > rowWidth && position.Y < columnHeight) {

                    position = new Point(position.X - rowWidth + HorizontalOffset, position.Y);
                    Size size = GetColumnSize();

                    foreach (var column in ColumnsPositions) {
                        Rect rect = GetRect(column.Value, size, false, false);

                        if (rect.Contains(position)) {

                            if (SelectedColumns.Contains(column)) {
                                SelectedColumns.Remove(column);

                                if (SelectingColumnWithItems) {
                                    foreach (var item in SelectedItems.Where(x => column.Value.IsValidateColumn(x.Value.Column)).ToArray())
                                        SelectedItems.Remove(item);

                                    RefreshVisualByCollection(SelectedItemBitmap, SelectedItems, SItems, GetItemSize(), GetItemsRenderBounds(), ref needSelectedItemsRendering);
                                }
                            }
                            else {
                                SelectedColumns.Add(column);

                                if (SelectingColumnWithItems) {
                                    foreach (var item in ItemsPositions.Where(x => ItemsFilter?.Invoke(x.Key) != false && column.Value.IsValidateColumnSpan(x.Value.Column, x.Value.FullColumn)))
                                        SelectedItems.Add(item);

                                    RefreshVisualByCollection(SelectedItemBitmap, SelectedItems, SItems, GetItemSize(), GetItemsRenderBounds(), ref needSelectedItemsRendering);
                                }
                            }

                            RefreshVisualByCollection(SelectedColumnBitmap, SelectedColumns, SColumns, GetColumnSize(), GetColumnRenderBounds(), ref needSelectedColumnsRendering);
                            break;
                        }
                    }

                    SelectedEventHandler?.Invoke(SelectedRows, SelectedColumns, SelectedColumns);
                }

                // find into items
                else if (position.X > rowWidth && position.Y > columnHeight) {

                    position = new Point(position.X - rowWidth + HorizontalOffset, position.Y - columnHeight + VerticalOffset);

                    var selectedItem = ItemsPositions.FirstOrDefault(x => GetRect(x.Value, GetItemSize(), false, false).Contains(position));

                    Size size = GetItemSize();

                    if (selectedItem.Key != null && ItemsFilter?.Invoke(selectedItem.Key) != false) {
                        if (SelectedItems.Contains(selectedItem))
                            SelectedItems.Remove(selectedItem);
                        else {
                            SelectedItems.Add(selectedItem);

                            if (SelectingItemWithRowsAndColumns) {
                                foreach (var row in RowsPositions.Where(x => selectedItem.Value.IsValidateRowSpan(x.Value.Row, x.Value.FullRow)))
                                    SelectedRows.Add(row);

                                foreach (var column in ColumnsPositions.Where(x => selectedItem.Value.IsValidateColumnSpan(x.Value.Column, x.Value.FullColumn)))
                                    SelectedColumns.Add(column);

                                RefreshVisualByCollection(SelectedRowBitmap, SelectedRows, SRows, GetRowSize(), GetRowsRenderBounds(), ref needSelectedRowsRendering)
                                .RefreshVisualByCollection(SelectedColumnBitmap, SelectedColumns, SColumns, GetColumnSize(), GetColumnRenderBounds(), ref needSelectedColumnsRendering);
                            }
                        }

                        LastSelectedItem = SelectedItems.LastOrDefault();

                        RefreshVisualByCollection(SelectedItemBitmap, SelectedItems, SItems, GetItemSize(), GetItemsRenderBounds(), ref needSelectedItemsRendering);

                        SelectedEventHandler?.Invoke(SelectedRows, SelectedColumns, SelectedColumns);
                    }
                    else if (AllowSelectingEmptyItem) {

                        Position originalPosition = new Position(position.Y / size.Height, position.X / size.Width, 1, 1);

                        var key = Guid.NewGuid();

                        if (SelectedItems.FirstOrDefault(x => x.Value.Compare(originalPosition)) is KeyValuePair<object, Position> s
                            && s.Key != null) {
                            EmptyPositions.Remove(s.Key);
                            SelectedItems.Remove(s);
                        }
                        else {
                            SelectedItems.Add(new KeyValuePair<object, Position>(key, originalPosition));
                            EmptyPositions[key] = originalPosition;
                        }

                        LastSelectedItem = SelectedItems.LastOrDefault();

                        RefreshVisualByCollection(SelectedItemBitmap, SelectedItems, SItems, GetItemSize(), GetItemsRenderBounds(), ref needSelectedItemsRendering);

                        SelectedEventHandler?.Invoke(SelectedRows, SelectedColumns, SelectedColumns);
                    }
                }
            }
        }

        protected override void OnMouseLeave(MouseEventArgs e) {
            base.OnMouseLeave(e);

            MouseOverElement = null;
            ClearMouseOverBitmap();
        }


        public override void LineLeft() {
            if (Keyboard.IsKeyDown(Key.Left)) {
                var lastSelected = GetLastSelectedPosition();

                if (lastSelected.Equals(default(KeyValuePair<object, Position>))) {
                    SelectedItems.Clear();
                    SelectedItems.Add(lastSelected = GetFirstForSelecting());
                }
                else {
                    // find next element on the left side ( 1 column)
                    SelectedItems.Clear();
                    SelectedItems.Add(lastSelected = GetLastSelectPosition(lastSelected, (item) => item.Value.Column < lastSelected.Value.Column
                    && item.Value.IsValidateRowSpan(lastSelected.Value.Row, lastSelected.Value.FullRow)));
                }

                SetScrolItemPosition(lastSelected.Value);

                RefreshVisualByCollection(SelectedItemBitmap, SelectedItems, SItems, GetItemSize(), GetItemsRenderBounds(), ref needSelectedItemsRendering);

                SelectedEventHandler?.Invoke(SelectedRows, SelectedColumns, SelectedColumns);
            }
            else base.LineLeft();
        }

        public override void LineRight() {
            if (Keyboard.IsKeyDown(Key.Right)) {
                var lastSelected = GetLastSelectedPosition();

                if (lastSelected.Equals(default(KeyValuePair<object, Position>))) {
                    SelectedItems.Clear();
                    SelectedItems.Add(lastSelected = GetFirstForSelecting());
                }
                else {
                    // find next element on the right side (+ 1 column)
                    SelectedItems.Clear();
                    SelectedItems.Add(lastSelected = GetNextSelectPosition(lastSelected, (item) => item.Value.Column > lastSelected.Value.Column
                    && item.Value.IsValidateRowSpan(lastSelected.Value.Row, lastSelected.Value.FullRow)));
                }

                SetScrolItemPosition(lastSelected.Value);

                RefreshVisualByCollection(SelectedItemBitmap, SelectedItems, SItems, GetItemSize(), GetItemsRenderBounds(), ref needSelectedItemsRendering);

                SelectedEventHandler?.Invoke(SelectedRows, SelectedColumns, SelectedColumns);
            }
            else base.LineRight();
        }

        public override void LineUp() {
            if (Keyboard.IsKeyDown(Key.Up)) {
                var lastSelected = GetLastSelectedPosition();

                if (lastSelected.Equals(default(KeyValuePair<object, Position>))) {
                    SelectedItems.Clear();
                    SelectedItems.Add(lastSelected = GetFirstForSelecting());
                }
                else {
                    // find next element on the top side (- 1 Row)
                    SelectedItems.Clear();
                    SelectedItems.Add(lastSelected = GetLastSelectPosition(lastSelected, (item) => item.Value.Row < lastSelected.Value.Row
                    && item.Value.IsValidateColumnSpan(lastSelected.Value.Column, lastSelected.Value.FullColumn)));
                }

                SetScrolItemPosition(lastSelected.Value);

                RefreshVisualByCollection(SelectedItemBitmap, SelectedItems, SItems, GetItemSize(), GetItemsRenderBounds(), ref needSelectedItemsRendering);

                SelectedEventHandler?.Invoke(SelectedRows, SelectedColumns, SelectedColumns);
            }
            else base.LineUp();
        }

        public override void LineDown() {
            if (Keyboard.IsKeyDown(Key.Down)) {

                var lastSelected = GetLastSelectedPosition();

                if (lastSelected.Equals(default(KeyValuePair<object, Position>))) {
                    SelectedItems.Clear();
                    SelectedItems.Add(lastSelected = GetFirstForSelecting());
                }
                else {
                    // find next element on the bottom side (+ 1 row)
                    SelectedItems.Clear();
                    SelectedItems.Add(lastSelected = GetNextSelectPosition(lastSelected, (item) => item.Value.Row > lastSelected.Value.Row
                    && item.Value.IsValidateColumnSpan(lastSelected.Value.Column, lastSelected.Value.FullColumn)));
                }

                SetScrolItemPosition(lastSelected.Value);

                RefreshVisualByCollection(SelectedItemBitmap, SelectedItems, SItems, GetItemSize(), GetItemsRenderBounds(), ref needSelectedItemsRendering);

                SelectedEventHandler?.Invoke(SelectedRows, SelectedColumns, SelectedColumns);
            }
            else
                base.LineDown();
        }


        protected override void OnScroll(double newValue, double oldValue, bool? horizontal = null) {

            ClearMouseOverBitmap();

            if (horizontal == null) return;

            double distance = newValue - oldValue;
            Rect distanceBounds = default(Rect);

            double hDistance = 0, vDistance = 0;

            if (horizontal == false) {
                vDistance = distance;
                Rect rowBounds = GetRowsRenderBounds();
                rowBounds.Offset(0, -distance);

                if (distance > 0)
                    distanceBounds = new Rect(rowBounds.Left, rowBounds.Bottom, rowBounds.Width, distance);
                else
                    distanceBounds = new Rect(rowBounds.Left, rowBounds.Top + distance, rowBounds.Width, Math.Abs(distance));

                RefreshRenderWithOffset(RowsBitmap, 0, -distance)
                    .RefreshVisuals(RowsBitmap, distanceBounds, Rows, RowsPositions, GetRowSize(), ref needRowsRendering, false);

                RefreshRenderWithOffset(SelectedRowBitmap, 0, -distance)
                    .RefreshSelectedVisual(SelectedRowBitmap, distanceBounds, SelectedRows, SRows, RowsPositions, GetRowSize(), ref needSelectedRowsRendering, false);

            }
            else if (horizontal == true) {
                hDistance = distance;
                Rect columnBounds = GetColumnRenderBounds();
                columnBounds.Offset(-distance, 0);

                if (distance > 0)
                    distanceBounds = new Rect(columnBounds.Right, columnBounds.Top, distance, columnBounds.Height);
                else
                    distanceBounds = new Rect(columnBounds.Left + distance, columnBounds.Top, Math.Abs(distance), columnBounds.Height);

                RefreshRenderWithOffset(ColumnsBitmap, -distance, 0)
                    .RefreshVisuals(ColumnsBitmap, distanceBounds, Columns, ColumnsPositions, GetColumnSize(), ref needColumnsRendering, false);

                RefreshRenderWithOffset(SelectedColumnBitmap, -distance, 0)
                    .RefreshSelectedVisual(SelectedColumnBitmap, distanceBounds, SelectedColumns, SColumns, ColumnsPositions, GetColumnSize(), ref needSelectedColumnsRendering, false);
            }

            Rect itemsBounds = GetItemsRenderBounds();
            itemsBounds.Offset(-hDistance, -vDistance);

            #region ItemsRefresh
            if (hDistance > 0)
                distanceBounds = new Rect(itemsBounds.Right, itemsBounds.Top, hDistance, itemsBounds.Height);
            else if (hDistance < 0)
                distanceBounds = new Rect(itemsBounds.Left + hDistance, itemsBounds.Top, Math.Abs(hDistance), itemsBounds.Height);
            else if (vDistance > 0)
                distanceBounds = new Rect(itemsBounds.Left, itemsBounds.Bottom, itemsBounds.Width, vDistance);
            else if (vDistance < 0)
                distanceBounds = new Rect(itemsBounds.Left, itemsBounds.Top + vDistance, itemsBounds.Width, Math.Abs(vDistance));

            RefreshRenderWithOffset(ItemsBitmap, -hDistance, -vDistance)
                .RefreshVisuals(ItemsBitmap, distanceBounds, Items, ItemsPositions, GetItemSize(), ref needItemsRendering, false, filter: ItemsFilter != null);

            RefreshRenderWithOffset(SelectedItemBitmap, -hDistance, -vDistance)
                .RefreshSelectedVisual(SelectedItemBitmap, distanceBounds, SelectedItems, SItems, ItemsPositions, GetItemSize(), ref needSelectedItemsRendering, false);
            #endregion

            gridTranslate.X = HorizontalOffset;
            gridTranslate.Y = VerticalOffset;

            gridInverseTranslate.X = -HorizontalOffset;
            gridInverseTranslate.Y = -VerticalOffset;
        }
        #endregion

        #region Helps
        private void Rendering(object sender, EventArgs e) {
            int con = ItemRenderQuantity;

            if (needItemsRendering) {

                foreach (var item in Items.Cast<object>().Take(con)) {

                    ItemsBitmap.Render(GetVisual(itemPresenter, GetRect(ItemsPositions[item], GetItemSize(), true, true), item));
                }

                Items.RemoveRange(0, Math.Min(Items.Count, con));

                if (Items.Count == 0)
                    needItemsRendering = false;
            }

            con = RowRenderQuantity;

            if (needRowsRendering) {

                Size size = GetRowSize();

                foreach (var row in Rows.Cast<object>().Take(con)) {

                    RowsBitmap.Render(GetVisual(rowPresenter, GetRect(RowsPositions[row], size, false, true), row));
                }

                Rows.RemoveRange(0, Math.Min(Rows.Count, con));

                if (Rows.Count == 0)
                    needRowsRendering = false;
            }

            con = ColumnRenderQuantity;

            if (needColumnsRendering) {

                Size size = GetColumnSize();

                foreach (var column in Columns.Cast<object>().Take(con)) {

                    ColumnsBitmap.Render(GetVisual(columnPresenter, GetRect(ColumnsPositions[column], size, true, false), column));
                }

                Columns.RemoveRange(0, Math.Min(Columns.Count, con));

                if (Columns.Count == 0)
                    needColumnsRendering = false;
            }


            con = SelectRenderQuantity;

            if (needSelectedItemsRendering) {

                foreach (var select in SItems.Cast<object>().Take(con)) {

                    if (ItemsPositions.ContainsKey(select))
                        SelectedItemBitmap.Render(GetVisual(selectedItemPresenter, GetRect(ItemsPositions[select], GetItemSize(), true, true), select));
                    else if (EmptyPositions.ContainsKey(select))
                        SelectedItemBitmap.Render(GetVisual(selectedItemPresenter, GetRect(EmptyPositions[select], GetItemSize(), true, true), select));
                }

                SItems.RemoveRange(0, Math.Min(SItems.Count, con));

                if (SItems.Count == 0)
                    needSelectedItemsRendering = false;
            }

            if (needSelectedRowsRendering) {

                Size size = GetRowSize();

                foreach (var select in SRows.Cast<object>().Take(con)) {

                    SelectedRowBitmap.Render(GetVisual(selectedRowPresenter, GetRect(RowsPositions[select], size, false, true), select));
                }

                SRows.RemoveRange(0, Math.Min(SRows.Count, con));

                if (SRows.Count == 0)
                    needSelectedRowsRendering = false;
            }

            if (needSelectedColumnsRendering) {

                Size size = GetColumnSize();

                foreach (var select in SColumns.Cast<object>().Take(con)) {

                    SelectedColumnBitmap.Render(GetVisual(selectedColumnPresenter, GetRect(ColumnsPositions[select], size, true, false), select));
                }

                SColumns.RemoveRange(0, Math.Min(SColumns.Count, con));

                if (SColumns.Count == 0)
                    needSelectedColumnsRendering = false;
            }
        }

        private ImageBrush GetImageBrush(RenderTargetBitmap bitmap) =>
            new ImageBrush { Stretch = Stretch.None, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top, TileMode = TileMode.None, ImageSource = bitmap };

        private Image GetImage(RenderTargetBitmap bitmap, double x, double y) {
            var result = new Image { Source = bitmap, Stretch = Stretch.None, Width = bitmap.Width, Height = bitmap.Height };
            result.Arrange(new Rect(new Point(x, y), new Size(result.Width, result.Height)));

            return result;
        }

        private Visual GetVisual(ContentPresenter presenter, Rect rect, object content) {
            presenter.Content = content;
            presenter.Width = rect.Width;
            presenter.Height = rect.Height;

            var tc = presenter.ContentTemplate;
            presenter.ContentTemplate = null;
            presenter.ContentTemplate = tc;

            var lc = presenter.ContentTemplateSelector;
            presenter.ContentTemplateSelector = null;
            presenter.ContentTemplateSelector = lc;

            presenter.Arrange(rect);
            
            return presenter;
        }

        private Visual GetBorderVisual(Rect rect, SolidColorBrush borderBrush, Thickness thickness) {
            var border = new Border { Width = rect.Width, Height = rect.Height, BorderBrush = borderBrush, BorderThickness = thickness };

            border.Arrange(rect);

            return border;
        }

        private Rect GetRect(Position postion, Size size, bool hOffset = false, bool voffset = false) => new Rect(postion.Column * size.Width - (hOffset ? HorizontalOffset : 0), postion.Row * size.Height - (voffset ? VerticalOffset : 0), postion.ColumnsSpan * size.Width, postion.RowsSpan * size.Height);

        private bool IsValidate(Rect element, Rect bounds) => Rect.Intersect(bounds, element) != Rect.Empty;


        private MultiPresentationBox SetScrollExtent() {

            if (RowsPositions.Any() && ColumnsPositions.Any()) {

                var rq = RowsPositions.Max(x => x.Value.FullRow);
                var cq = ColumnsPositions.Max(x => x.Value.FullColumn);

                Size size = GetItemSize();

                SetScrollExtent(cq * size.Width + GetRowWidth(), rq * size.Height + GetColumnHeight(), RenderSize.Width - GetRowWidth(), RenderSize.Height - GetColumnHeight());
            }

            return this;
        }

        private MultiPresentationBox SetScrolItemPosition(Position position) {
            Size size = GetItemSize();

            Rect rect = GetRect(position, size);

            Rect renderBounds = GetItemsRenderBounds();

            double xOffset = this.HorizontalOffset;
            double yOffset = this.VerticalOffset;

            // left <-> right
            if (rect.Right > renderBounds.Right)
                xOffset = rect.Left - (ViewportWidth - GetRowWidth()) + rect.Width * 1.5;
            else if (rect.Left < renderBounds.Left)
                xOffset = rect.Left;

            // top <-> bottom
            if (rect.Bottom > renderBounds.Bottom)
                yOffset = rect.Top - (ViewportHeight - GetColumnHeight()) + rect.Height * 1.5;
            else if (rect.Top < renderBounds.Top)
                yOffset = rect.Top;

            SetHorizontalOffset(xOffset);
            SetVerticalOffset(yOffset);

            return this;
        }

        private MultiPresentationBox RebuildRenderMap(Size size) {

            int rowWidth = GetRowWidth();
            int columnHeight = GetColumnHeight();

            ItemsBitmap?.Clear();
            ItemsBitmap = new RenderTargetBitmap(Math.Max(1, (int)size.Width - rowWidth), Math.Max(1, (int)size.Height - columnHeight), 96, 96, PixelFormats.Default);
            ItemsBrush = GetImageBrush(ItemsBitmap);

            RowsBitmap?.Clear();
            RowsBitmap = new RenderTargetBitmap(Math.Max(1, rowWidth), Math.Max(1, (int)size.Height - columnHeight), 96, 96, PixelFormats.Default);
            RowsBrush = GetImageBrush(RowsBitmap);

            ColumnsBitmap?.Clear();
            ColumnsBitmap = new RenderTargetBitmap(Math.Max(1, (int)size.Width - rowWidth), Math.Max(1, columnHeight), 96, 96, PixelFormats.Default);
            ColumnsBrush = GetImageBrush(ColumnsBitmap);

            SelectedItemBitmap?.Clear();
            SelectedItemBitmap = (RenderTargetBitmap)ItemsBitmap.Clone();
            SelectedItemsBrush = GetImageBrush(SelectedItemBitmap);

            SelectedRowBitmap?.Clear();
            SelectedRowBitmap = (RenderTargetBitmap)RowsBitmap.Clone();
            SelectedRowsBrush = GetImageBrush(SelectedRowBitmap);

            SelectedColumnBitmap?.Clear();
            SelectedColumnBitmap = (RenderTargetBitmap)ColumnsBitmap.Clone();
            SelectedColumnsBrush = GetImageBrush(SelectedColumnBitmap);

            MouseOverItemsBitmap?.Clear();
            MouseOverItemsBitmap = (RenderTargetBitmap)ItemsBitmap.Clone();
            MouseOverItemsBrush = GetImageBrush(MouseOverItemsBitmap);

            MouseOverRowsBitmap?.Clear();
            MouseOverRowsBitmap = (RenderTargetBitmap)RowsBitmap.Clone();
            MouseOverRowsBrush = GetImageBrush(MouseOverRowsBitmap);

            MouseOverColumnsBitmap?.Clear();
            MouseOverColumnsBitmap = (RenderTargetBitmap)ColumnsBitmap.Clone();
            MouseOverColumnsBrush = GetImageBrush(MouseOverColumnsBitmap);

            HighlightBitmap?.Clear();
            HighlightBitmap = new RenderTargetBitmap((int)Math.Max(1, this.RenderSize.Width), (int)Math.Max(1, this.RenderSize.Height), 96, 96, PixelFormats.Default);
            HighlightBrush = GetImageBrush(HighlightBitmap);

            this.InvalidateVisual();

            return this;
        }


        private MultiPresentationBox RefreshRenderWithOffset(RenderTargetBitmap render, double horizontalOffset, double verticalOffset) {

            var copy = (RenderTargetBitmap)render.Clone();

            render.Clear();

            render.Render(GetImage(copy, horizontalOffset, verticalOffset));

            return this;
        }

        private MultiPresentationBox RefreshVisuals(RenderTargetBitmap bitmap, Rect bound,
            ArrayList visuals, Dictionary<object, Position> positions, Size elementSize,
            ref bool needRendering, bool clearBitmap = true, bool stopRender = false, bool filter = false) {

            if (clearBitmap)
                bitmap?.Clear();

            if (stopRender) {
                needRendering = false;
                visuals.Clear();
            }

            if (positions == null || !positions.Any()) return this;

            foreach (var element in positions) {

                Rect rect = GetRect(element.Value, elementSize);

                if (IsValidate(rect, bound))
                    if (!filter)
                        visuals.Add(element.Key);
                    else if (ItemsFilter(element.Key))
                        visuals.Add(element.Key);
            }

            needRendering = true;

            return this;
        }

        private MultiPresentationBox RefreshSelectedVisual(RenderTargetBitmap bitmap, Rect bound,
            ObservableCollection<KeyValuePair<object, Position>> selectedCollection,
            ArrayList visuals, Dictionary<object, Position> positions, Size elementSize,
            ref bool needRendering, bool clearBitmap = true, bool stopRender = false) {

            if (clearBitmap)
                bitmap?.Clear();

            if (stopRender) {
                needRendering = false;
                visuals.Clear();
            }

            if (positions == null || !positions.Any() || !selectedCollection.Any()) return this;

            foreach (var select in selectedCollection) {

                Rect rect = GetRect(select.Value, elementSize);

                if (IsValidate(rect, bound))
                    visuals.Add(select.Key);

            }

            needRendering = true;

            return this;
        }

        private MultiPresentationBox SetItemPositions() {
            ItemsPositions.Clear();

            if (ItemsSource == null || ItemPositionFunc == null || !RowsPositions.Any() || !ColumnsPositions.Any()) return this;

            int[,] placement = null;

            if (ItemPlacingFunc != null)
                placement = new int[RowsPositions.Max(x => x.Value.FullRow) + 1, ColumnsPositions.Max(x => x.Value.FullColumn) + 1];

            foreach (var item in ItemsSource) {
                if (ItemPositionFunc(RowsPositions, ColumnsPositions, item) is Position p) {

                    Dictionary<object, int> rowSpans = RowsPositions.ToDictionary(x => x.Key, x => x.Value.RowsSpan);
                    var columnSpans = ColumnsPositions.ToDictionary(x => x.Key, x => x.Value.ColumnsSpan);

                    if (ItemPlacingFunc != null) {
                        if (ItemPlacingFunc(placement, rowSpans, columnSpans, item, p) is Position ps) {

                            for (int i = ps.Row; i < ps.FullRow; i++)
                                for (int j = ps.Column; j < ps.FullColumn; j++)
                                    placement[i, j]++;

                            ItemsPositions[item] = ps;

                        }
                    }
                    else
                        ItemsPositions[item] = p;
                }
            }

            placement = null;

            return this;
        }

        private MultiPresentationBox SetPositions(Dictionary<object, Position> positions,
            IEnumerable source,
            Func<IEnumerable, object, Position, object, Position> positionFunc, Func<Dictionary<object, Position>, int> groupsFunc, out int spanRendering) {
            positions.Clear();

            spanRendering = 1;

            if (source == null || positionFunc == null) return this;

            Position lastPosition = default(Position);
            object lastElement = null;

            foreach (var element in source) {
                lastPosition = positions[element] = positionFunc(source, element, lastPosition, lastElement);

                lastElement = element;
            }

            if (groupsFunc != null)
                spanRendering = groupsFunc.Invoke(positions) + 1;

            return this;
        }

        private MultiPresentationBox SetOffsetPositions(Dictionary<object, Position> positions, int verticalOffset, int horizontalOffset) {

            if (!positions.Any()) return this;

            foreach (var position in positions.ToArray())
                positions[position.Key] = position.Value.Offset(verticalOffset, horizontalOffset);

            return this;
        }


        private MultiPresentationBox RefreshVisualByCollection(RenderTargetBitmap bitmap,
            ObservableCollection<KeyValuePair<object, Position>> elements,
            ArrayList visuals,
            Size size,
            Rect bounds,
            ref bool needRendering) {

            needRendering = false;
            bitmap?.Clear();

            if (!elements.Any()) return this;

            foreach (var element in elements)
                if (IsValidate(GetRect(element.Value, size, false, false), bounds)) visuals.Add(element.Key);

            needRendering = true;

            return this;
        }

        private MultiPresentationBox ClearMouseOverBitmap() {
            MouseOverItemsBitmap?.Clear();
            MouseOverRowsBitmap?.Clear();
            MouseOverColumnsBitmap?.Clear();
            return this;
        }

        private MultiPresentationBox ClearHightlightBitmap() {
            HighlightBitmap?.Clear();
            return this;
        }

        private void RefreshGridBrush(VisualBrush brush, Size size, Brush lineColor, Thickness thickness) {
            brush.Visual = new Border { BorderBrush = lineColor, BorderThickness = thickness, Width = size.Width, Height = size.Height };
            brush.Viewport = new Rect(size);
        }

        private MultiPresentationBox ClearAllSelecting(bool refresh = false) {
            this.SelectedItems.Clear();
            this.SelectedRows.Clear();
            this.SelectedColumns.Clear();

            if (refresh) {
                RefreshVisualByCollection(SelectedItemBitmap, SelectedItems, SItems, GetItemSize(), GetItemsRenderBounds(), ref needSelectedItemsRendering)
                .RefreshVisualByCollection(SelectedRowBitmap, SelectedRows, SRows, GetRowSize(), GetRowsRenderBounds(), ref needSelectedRowsRendering)
                .RefreshVisualByCollection(SelectedColumnBitmap, SelectedColumns, SColumns, GetColumnSize(), GetColumnRenderBounds(), ref needSelectedColumnsRendering);

                SelectedEventHandler?.Invoke(SelectedRows, SelectedColumns, SelectedColumns);
            }

            return this;
        }

        private KeyValuePair<object, Position> GetLastSelectedPosition() {
            if (SelectedItems.Any()) return SelectedItems.Last();
            else return default(KeyValuePair<object, Position>);
        }

        private KeyValuePair<object, Position> GetFirstForSelecting() {

            Position bounds = new Position(VerticalOffset / ItemSize.Width, HorizontalOffset / ItemSize.Height, 1 + ItemsBitmap.Width / ItemSize.Width, 1 + ItemsBitmap.Height / ItemSize.Height);

            return ItemsPositions.Where(x => ItemsFilter?.Invoke(x.Key) != false).OrderBy(x => x.Value.Row + x.Value.Column).First();
        }

        private KeyValuePair<object, Position> GetNextSelectPosition(KeyValuePair<object, Position> lastPosition, Predicate<KeyValuePair<object, Position>> finder) {

            var nextElement = ItemsPositions.Where(x => ItemsFilter?.Invoke(x.Key) != false).OrderBy(x => x.Value.Row).ThenBy(x => x.Value.Column).FirstOrDefault(x => finder(x));

            if (nextElement.Key != null)
                return nextElement;
            else
                return lastPosition;
        }

        private KeyValuePair<object, Position> GetLastSelectPosition(KeyValuePair<object, Position> lastPosition, Predicate<KeyValuePair<object, Position>> finder) {

            var nextElement = ItemsPositions.Where(x => ItemsFilter?.Invoke(x.Key) != false).OrderBy(x => x.Value.Row).ThenBy(x => x.Value.Column).LastOrDefault(x => finder(x));

            if (nextElement.Key != null)
                return nextElement;
            else
                return lastPosition;
        }


        private Rect GetItemsRenderBounds() => ItemsBitmap != null ? new Rect(HorizontalOffset, VerticalOffset, ItemsBitmap.Width, ItemsBitmap.Height) : default(Rect);

        private Rect GetRowsRenderBounds() => RowsBitmap != null ? new Rect(0, VerticalOffset, RowsBitmap.Width, RowsBitmap.Height) : default(Rect);

        private Rect GetColumnRenderBounds() => ColumnsBitmap != null ? new Rect(HorizontalOffset, 0, ColumnsBitmap.Width, ColumnsBitmap.Height) : default(Rect);


        private Size GetItemSize() => ItemSize;


        private Size GetRowSize() => new Size(RowWidth, ItemSize.Height);

        private Size GetColumnSize() => new Size(ItemSize.Width, ColumnsHeight);


        private int GetRowWidth() => (int)(RowWidth * HorizontalSpanRowRendering);

        private int GetColumnHeight() => (int)(ColumnsHeight * VerticalSpanColumnRendering);


        private static Dictionary<object, Position> GetDeaultNewDictionary() => new Dictionary<object, Position>();

        public Dictionary<object, Position> GetItemsPosition() => this.ItemsPositions;
        #endregion
    }
}
