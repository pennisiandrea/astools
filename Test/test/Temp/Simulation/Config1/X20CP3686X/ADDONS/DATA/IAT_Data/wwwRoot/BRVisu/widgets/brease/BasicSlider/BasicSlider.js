define([
    'brease/core/BaseWidget',
    'brease/events/BreaseEvent',
    'widgets/brease/BasicSlider/libs/Renderer',
    'brease/enum/Enum',
    'brease/core/Types',
    'brease/core/Utils',
    'brease/decorators/MeasurementSystemDependency',
    'brease/datatype/Node',
    'brease/config/NumberFormat',
    'brease/decorators/LanguageDependency',
    'widgets/brease/BasicSlider/libs/Config',
    'widgets/brease/common/libs/wfUtils/UtilsEditableBinding',
    'brease/decorators/DragAndDropCapability',
    'widgets/brease/common/libs/BreaseResizeObserver',
    'brease/decorators/ContentActivatedDependency',
    'widgets/brease/common/DragDropProperties/libs/DroppablePropertiesEvents'
], function (SuperClass, BreaseEvent, Renderer, Enum, Types, Utils, measurementSystemDependency, Node, NumberFormat, languageDependency, Config, UtilsEditableBinding, dragAndDropCapability, BreaseResizeObserver, contentActivatedDependency) {

    'use strict';

    /**
     * @class widgets.brease.BasicSlider
     * BasicSlider widget. Input value via slider
     * @extends brease.core.BaseWidget
     *
     * @mixins widgets.brease.common.DragDropProperties.libs.DroppablePropertiesEvents
     *
     * @iatMeta category:Category
     * Numeric
     * @iatMeta description:short
     * Schieberegler horizontal/vertical
     * @iatMeta description:de
     * Ermöglicht dem Benutzer einen numerischen Wert mit einem Schiebeschalter zu verändern
     * @iatMeta description:en
     * Enables the user to change a numeric value by a slider
     */

    var defaultSettings = Config,

        WidgetClass = SuperClass.extend(function BasicSlider() {
            SuperClass.apply(this, arguments);
        }, defaultSettings),

        p = WidgetClass.prototype;

    WidgetClass.static.multitouch = true;

    p.init = function () {

        if (this.settings.omitClass !== true) {
            this.addInitialClass('breaseBasicSlider');
        }
        this.triggerValueChangedEventThrottled = _.throttle(this.triggerValueChangedEvent, 100);

        this.data = {
            node: new Node(this.settings.value, null, this.settings.minValue, this.settings.maxValue)
        };

        this.initSettings();
        this.observer = new BreaseResizeObserver(this.elem, this._bind(this.redraw));
        SuperClass.prototype.init.apply(this, arguments);
        this.renderer = new Renderer(this);
        this.onMove = false;
        this._addSliderThumpEvents();
        if (this.settings.minValue > 0) {
            this.settings.value = this.settings.minValue;
            this.submitChange();
        }

        this.writeUnit(this.settings.unitSymbol);
        this.writeValue();
        document.body.addEventListener(BreaseEvent.THEME_CHANGED, this._bind(_onThemeChanged));
    };

    p._addSliderThumpEvents = function () {
        this.sliderThumb = this.el.find('.sliderThumb');
        // prevent parent gestures when the user interacts with the thumb (e.g.: ContentCarousel)
        this.sliderThumb.on('mousedown pointerdown touchstart', this._bind(_stopEventPropagation));
        this.sliderThumb.on(BreaseEvent.MOUSE_DOWN, this._bind('_mouseDownHandler'));
    };

    p._enableHandler = function () {
        if (this.renderer) {
            this.renderer.enableClassHandling();
        }
    };

    p.setStyle = function (style) {
        SuperClass.prototype.setStyle.call(this, style);
        this.redrawView();
        this.writeValue();
    };

    /**
     * @method setFormat
     * Sets format
     * @param {brease.config.MeasurementSystemFormat} format
     */
    p.setFormat = function (format) {
        this.settings.format = format;
        this.updateFormat();
        this.renderer.updateAxis();
        this.writeValue();
    };

    /**
     * @method getFormat 
     * Returns format.
     * @return {brease.config.MeasurementSystemFormat}
     */
    p.getFormat = function () {
        return this.settings.format;
    };

    /**
     * @method setLargeChange
     * Sets largeChange
     * @param {UNumber} largeChange
     */
    p.setLargeChange = function (largeChange) {
        this.settings.largeChange = largeChange;
    };

    /**
     * @method getLargeChange 
     * Returns largeChange.
     * @return {UNumber}
     */
    p.getLargeChange = function () {
        return this.settings.largeChange;
    };

    /**
     * @method setMajorTicks
     * Sets majorTicks
     * @param {UInteger} majorTicks
     */
    p.setMajorTicks = function (majorTicks) {
        this.settings.majorTicks = majorTicks;
        this.renderer.redraw();
    };

    /**
     * @method getMajorTicks 
     * Returns majorTicks.
     * @return {UInteger}
     */
    p.getMajorTicks = function () {
        return this.settings.majorTicks;
    };

    /**
     * @method setMaxValue
     * Sets maxValue
     * @param {Number} maxValue
     */
    p.setMaxValue = function (maxValue) {

        if (Utils.isNumeric(maxValue)) {
            this.data.node.setMaxValue(maxValue);
            this.renderer.settings.maxValue = maxValue;
            this.renderer.scaleDomain(this.data.node);
            this.renderer.updateAxis();
            this.setValue(this.data.node.value);
        }
    };

    /**
     * @method getMaxValue 
     * Returns maxValue.
     * @return {Number} maxValue
     */
    p.getMaxValue = function () {
        return this.data.node.maxValue;
    };

    /**
     * @method setMinValue
     * Sets minValue
     * @param {Number} minValue
     */
    p.setMinValue = function (minValue) {

        if (Utils.isNumeric(minValue)) {
            this.data.node.setMinValue(minValue);
            this.renderer.settings.minValue = minValue;
            this.renderer.scaleDomain(this.data.node);
            this.renderer.updateAxis();
            this.setValue(this.data.node.value);
        }
    };

    /**
     * @method getMinValue 
     * Returns minValue.
     * @return {Number} minValue
     */
    p.getMinValue = function () {
        return this.data.node.minValue;
    };

    /**
     * @method setNode
     * Sets node
     * @param {brease.datatype.Node} node
     */
    p.setNode = function (node) {
        if (node.minValue !== undefined) {
            if (this.data.node.minValue !== node.minValue) {
                this.data.node.setMinValue(node.minValue);
                this.renderer.scaleDomain(node);
                this.renderer.updateAxis();
            }
        }
        if (node.maxValue !== undefined) {
            if (this.data.node.maxValue !== node.maxValue) {
                this.data.node.setMaxValue(node.maxValue);
                this.renderer.scaleDomain(node);
                this.renderer.updateAxis();
            }
        }

        if (this.valueChange !== undefined && this.valueChange.state() !== 'resolved') {
            this.valueChange.resolve();
        } else {
            this.data.node.setId(node.id);
            this.setValue(node.value);
        }

    };

    /**
     * @method getNode 
     * Returns node.
     * @return {brease.datatype.Node}
     */
    p.getNode = function () {
        return this.data.node;
    };

    /**
     * @method setOrientation
     * Sets orientation
     * @param {brease.enum.Orientation} orientation
     */
    p.setOrientation = function (orientation) {
        this.settings.orientation = orientation;
        this.setOrientationClasses();
        this.renderer.redraw();
    };

    /**
     * @method getOrientation 
     * Returns orientation.
     * @return {brease.enum.Orientation}
     */
    p.getOrientation = function () {
        return this.settings.orientation;
    };

    /**
     * @method setShowTickNumbers
     * Sets showTickNumbers
     * @param {Boolean} showTickNumbers
     */
    p.setShowTickNumbers = function (showTickNumbers) {
        this.settings.showTickNumbers = showTickNumbers;
        this.renderer.tickNumbersHandling();
    };

    /**
     * @method getShowTickNumbers 
     * Returns showTickNumbers.
     * @return {Boolean}
     */
    p.getShowTickNumbers = function () {
        return this.settings.showTickNumbers;
    };

    /**
     * @method setShowValueDisplay
     * Sets showValueDisplay
     * @param {Boolean} showValueDisplay
     */
    p.setShowValueDisplay = function (showValueDisplay) {
        this.settings.showValueDisplay = showValueDisplay;
        this.renderer.valueDisplayHandling();
    };

    /**
     * @method getShowValueDisplay 
     * Returns showValueDisplay.
     * @return {Boolean}
     */
    p.getShowValueDisplay = function () {
        return this.settings.showValueDisplay;
    };

    /**
     * @method setSmallChange
     * Sets smallChange
     * @param {UNumber} smallChange
     */
    p.setSmallChange = function (smallChange) {
        this.settings.smallChange = smallChange;
    };

    /**
     * @method getSmallChange 
     * Returns smallChange.
     * @return {UNumber}
     */
    p.getSmallChange = function () {
        return this.settings.smallChange;
    };

    /**
     * @method setTickPosition
     * Sets tickPosition
     * @param {brease.enum.TickStyle} tickPosition
     */
    p.setTickPosition = function (tickPosition) {
        this.settings.tickPosition = tickPosition;
        this.renderer.tickPositionHandling();
    };

    /**
     * @method getTickPosition 
     * Returns tickPosition.
     * @return {brease.enum.TickStyle}
     */
    p.getTickPosition = function () {
        return this.settings.tickPosition;
    };

    /**
     * @method setShowUnit
     * Sets showUnit
     * @param {Boolean} showUnit
     */
    p.setShowUnit = function (showUnit) {
        this.settings.showUnit = showUnit;
        this.showUnit();
    };

    /**
     * @method getShowUnit 
     * Returns showUnit.
     * @return {Boolean}
     */
    p.getShowUnit = function () {
        return this.settings.showUnit;
    };

    /**
     * @method setUnit
     * Sets unit
     * @param {brease.config.MeasurementSystemUnit} unit
     */
    p.setUnit = function (unit) {
        if (Utils.isObject(unit)) {
            this.settings.unitTextKey = undefined;
            this.settings.unit = unit;
        } else {
            if (brease.language.isKey(unit)) {
                try {
                    this.setLangDependency(true);
                    this.settings.unitTextKey = brease.language.parseKey(unit);
                    this.settings.unit = JSON.parse(brease.language.getTextByKey(this.settings.unitTextKey).replace(/'/g, '"'));
                } catch (error) {
                    console.iatWarn(this.elem.id + ':Unit String "' + unit + '" is invalid!');
                }
            } else {
                this.settings.unitTextKey = undefined;
                try {
                    this.settings.unit = JSON.parse(unit.replace(/'/g, '"'));
                } catch (error) {
                    console.iatWarn(this.elem.id + ':Unit String "' + unit + '" is invalid!');
                }
            }
        }
        if (unit === undefined) {
            this.settings.unit = undefined;
        }

        this.processMeasurementSystemUpdate();

    };

    /**
     * @method getUnit 
     * Returns unit.
     * @return {brease.config.MeasurementSystemUnit}
     */
    p.getUnit = function () {
        return this.settings.unit;
    };

    /**
     * @method setValue
     * sets the value
     * @iatStudioExposed
     * @param {Number} value
     */
    p.setValue = function (value) {

        if (this.onMove === false) {
            if (value !== undefined) {
                this.data.node.setValue(Types.parseValue(value, 'Number', { min: this.data.node.minValue, max: this.data.node.maxValue }));
            }

            if (this.valueChange !== undefined && this.valueChange.state() !== 'resolved') {
                this.valueChange.resolve();
            } else {
                this.renderer.updateThumbPositionByValue(this.data.node.value);
                this.writeValue();
            }
        }
    };

    /**
     * @method getValue
     * get the value
     * @iatStudioExposed
     * @return {Number} value
     */
    p.getValue = function () {
        return this.data.node.value;
    };

    /**
     * @method setScaleOffset
     * Sets scaleOffset
     * @param {PixelVal} scaleOffset
     */
    p.setScaleOffset = function (scaleOffset) {
        this.settings.scaleOffset = parseInt(scaleOffset, 10);
        this.renderer.redraw();
    };

    /**
     * @method getScaleOffset 
     * Returns scaleOffset.
     * @return {PixelVal}
     */
    p.getScaleOffset = function () {
        return parseInt(this.settings.scaleOffset, 10) + 'px';
    };

    /**
     * @method setThumbSize
     * Sets thumbSize
     * @param {PixelVal} thumbSize
     */
    p.setThumbSize = function (thumbSize) {
        this.settings.thumbSize = parseInt(thumbSize, 10);
        this.renderer.redraw();
    };

    /**
     * @method getThumbSize 
     * Returns thumbSize.
     * @return {PixelVal}
     */
    p.getThumbSize = function () {
        return parseInt(this.settings.thumbSize, 10) + 'px';
    };

    /**
     * @method setValueDisplaySize
     * Sets valueDisplaySize
     * @param {PixelVal} valueDisplaySize
     */
    p.setValueDisplaySize = function (valueDisplaySize) {
        this.settings.valueDisplaySize = parseInt(valueDisplaySize, 10);
        this.renderer.redraw();
    };

    /**
     * @method getValueDisplaySize 
     * Returns valueDisplaySize.
     * @return {PixelVal}
     */
    p.getValueDisplaySize = function () {
        return parseInt(this.settings.valueDisplaySize, 10) + 'px';
    };

    /**
     * @method setEllipsis
     * Sets ellipsis
     * @param {Boolean} ellipsis
     */
    p.setEllipsis = function (ellipsis) {
        this.settings.ellipsis = ellipsis;
        this.renderer.redraw();
    };

    /**
     * @method getEllipsis 
     * Returns ellipsis.
     * @return {Boolean}
     */
    p.getEllipsis = function () {
        return this.settings.ellipsis;
    };

    /**
     * @method setTrackSize
     * Sets trackSize
     * @param {PixelVal} trackSize
     */
    p.setTrackSize = function (trackSize) {
        this.settings.trackSize = parseInt(trackSize, 10);
        this.renderer.redraw();
    };

    /**
     * @method getTrackSize 
     * Returns trackSize.
     * @return {PixelVal}
     */
    p.getTrackSize = function () {
        return parseInt(this.settings.trackSize, 10) + 'px';
    };

    /**
     * @method setImage
     * @iatStudioExposed
     * Sets image
     * @param {ImagePath} image
     */
    p.setImage = function (image) {
        this.settings.image = image;
        var imgEl = this.el.find('img');

        if (image !== undefined && image !== '') {
            
            if (imgEl.length > 0) {
                this.renderer.updateImg(image, false);
            } else {
                this.renderer.updateImg(image, true);
            }
        } else {
            imgEl.remove();
            this.renderer.thumbCornerHandling(false);
        }
    };

    /**
     * @method getImage 
     * Returns image.
     * @return {ImagePath}
     */
    p.getImage = function () {
        return this.settings.image;
    };

    /**
     * @method setChangeOnMove
     * Sets changeOnMove
     * @param {Boolean} changeOnMove
     */
    p.setChangeOnMove = function (changeOnMove) {
        this.settings.changeOnMove = changeOnMove;
    };

    /**
     * @method getChangeOnMove 
     * Returns changeOnMove.
     * @return {Boolean}
     */
    p.getChangeOnMove = function () {
        return this.settings.changeOnMove;
    };

    p.setEditable = function (editable, metaData) {
        var editableBindings = ['value', 'node'];
        UtilsEditableBinding.handleEditable(editable, metaData, this, editableBindings);
    };

    p.langChangeHandler = function (e) {
        if (this.data.node.unit !== null && this.settings.showUnit === true) {
            this.showUnit();
        }
    };

    p.showUnit = function () {
        brease.language.pipeAsyncUnitSymbol(this.data.node.unit, this._bind('writeUnit'));
    };

    p.writeUnit = function (symbol) {
        this.settings.unitSymbol = symbol;
        if (this.settings.showUnit === true || this.settings.showUnit === 'true') {
            if (this.renderer) {
                this.renderer.outputUnit.text(symbol);
                if (brease.config.editMode === true) {
                    this.renderer.outputUnit.text('unit');
                }
            }            
        } else {
            if (this.renderer) {
                this.renderer.outputUnit.text('');
            }
        }
    };

    p.processMeasurementSystemUpdate = function () {

        var widget = this;

        this.settings.mms = brease.measurementSystem.getCurrentMeasurementSystem();
        this.updateFormat();

        this.valueChange = $.Deferred();
        this.unitChange = $.Deferred();

        $.when(
            this.valueChange.promise(),
            this.unitChange.promise()
        ).then(function successHandler() {
            _updateNodeDisplay(widget);
        });

        var previousUnitCode = this.data.node.unit;
        if (this.settings.unit !== undefined) {
            this.data.node.setUnit(this.settings.unit[this.settings.mms]);
        }
        if (this.data.node.unit !== previousUnitCode) {
            brease.language.pipeAsyncUnitSymbol(this.data.node.unit, this._bind(_setUnitSymbol));
        } else {
            this.unitChange.resolve();
        }

        var subscriptions = brease.uiController.getSubscriptionsForElement(this.elem.id);
        if (subscriptions !== undefined && subscriptions.node !== undefined) {
            if (this.data.node.unit !== previousUnitCode) {
                this.sendNodeChange({ attribute: 'node', nodeAttribute: 'unit', value: this.data.node.unit });
            } else {
                this.valueChange.resolve();
            }
        } else {
            this.valueChange.resolve();
        }
    };

    p.measurementSystemChangeHandler = function () {
        this.processMeasurementSystemUpdate();
    };

    p.contentActivatedHandler = function () {
        if (this.observer.initialized) {
            this.observer.wake();
        } else {
            this.observer.init();
        }
    };

    p.writeValue = function () {
        this.settings.displayedValue = brease.formatter.formatNumber(this.data.node.value, this.settings.numberFormat, this.settings.useDigitGrouping, this.settings.separators);
        this.renderer.outputVal.text(this.settings.displayedValue);
    };

    p._mouseDownHandler = function (e) {
        if (this.isDisabled || this.onMove) { return; }
        this.onMove = true;
        this.pointerId = Utils.getPointerId(e);

        this._handleEvent(e);
        $(document).on(BreaseEvent.MOUSE_MOVE, this._bind('_mouseMoveHandler'));
        $(document).on(BreaseEvent.MOUSE_UP, this._bind('_mouseUpHandler'));

        /**
         * @event MouseDown
         * @iatStudioExposed
         * Fired when widget enters mouseDown state
         * @param {String} horizontalPos horizontal position of mouse in pixel i.e '10px'
         * @param {String} verticalPos vertical position of mouse in pixel i.e '10px'
         */
        var clickEv = this.createMouseEvent('MouseDown', {}, e);
        clickEv.dispatch();
    };

    p._mouseMoveHandler = function (e) {

        this._handleEvent(e);
        this.renderer.getCurrentValue();
        this.writeValue();

        if (this.settings.changeOnMove === true) {
            this.submitChange();
        }
    };

    p._mouseUpHandler = function (e) {
        if (this.isDisabled || brease.config.editMode || Utils.getPointerId(e) !== this.pointerId) { return; }
        this._handleEvent(e);
        $(document).off(BreaseEvent.MOUSE_MOVE, this._bind('_mouseMoveHandler'));
        $(document).off(BreaseEvent.MOUSE_UP, this._bind('_mouseUpHandler'));

        this.renderer.getCurrentValue();
        this.writeValue();

        this.submitChange();
        this.onMove = false;

        /**
         * @event MouseUp
         * @iatStudioExposed
         * Fired when widget enters mouseUp state
         * @param {String} horizontalPos horizontal position of mouse in pixel i.e '10px'
         * @param {String} verticalPos vertical position of mouse in pixel i.e '10px'
         */
        var clickEv = this.createMouseEvent('MouseUp', {}, e);
        clickEv.dispatch();
    };

    p._visibleHandler = function () { 
        SuperClass.prototype._visibleHandler.apply(this, arguments);
        // if slider is in a tabitem it can happen that it has display:none during init
        // it tab gets visible we have to redraw it to calculate the correct dimensions
        if (this.isVisible() && this.renderer) {
            this.redraw();
        }
    };

    p.submitChange = function () {
        this.sendValueChange({ value: this.getValue(), node: this.getNode() });

        this.triggerValueChangedEventThrottled();
    };

    p.triggerValueChangedEvent = function () {
        /**
         * @event ValueChanged
         * @iatStudioExposed
         * Fired when index changes.
         * @param {Number} value
         */
        var clickEv = this.createEvent('ValueChanged', { value: this.getValue() });
        clickEv.dispatch();
    };
    
    p.wake = function () {
        this._addRedrawListener();
        document.body.addEventListener(BreaseEvent.THEME_CHANGED, this._bind(_onThemeChanged));
        SuperClass.prototype.wake.apply(this, arguments);
    };

    p.suspend = function () {
        document.body.removeEventListener(BreaseEvent.THEME_CHANGED, this._bind(_onThemeChanged));
        this.observer.suspend();
        this._removeRedrawListener();
        SuperClass.prototype.suspend.apply(this, arguments);
    };

    p.dispose = function () {
        document.body.removeEventListener(BreaseEvent.THEME_CHANGED, this._bind(_onThemeChanged));
        this.observer.dispose();
        this.observer = undefined;
        this.sliderThumb.off();
        this.renderer.dispose();
        this.triggerValueChangedEventThrottled.cancel();
        this._removeRedrawListener();

        $(document).off(BreaseEvent.MOUSE_UP, this._bind('_mouseUpHandler'));
        $(document).off(BreaseEvent.MOUSE_MOVE, this._bind('_mouseMoveHandler'));
        SuperClass.prototype.dispose.apply(this, arguments);
    };

    function reDrawListener(e) {
        if (e.detail.contentId === this.getParentContentId()) {
            this.redrawView();
        }
    }

    p._addRedrawListener = function () {
        if (isNaN(this.settings.height) || isNaN(this.settings.width)) {
            brease.bodyEl.on(BreaseEvent.FRAGMENT_SHOW, this._bind(reDrawListener));
        }
    };

    p._removeRedrawListener = function () {
        brease.bodyEl.off(BreaseEvent.FRAGMENT_SHOW, this._bind(reDrawListener));
    };

    p.setOrientationClasses = function () {
        switch (this.settings.orientation) {
            case Enum.Orientation.LTR: case Enum.Orientation.RTL:
                this.el.addClass('horizontal').removeClass('vertical');
                break;
            case Enum.Orientation.TTB: case Enum.Orientation.BTT:
                this.el.addClass('vertical').removeClass('horizontal');
                break;
        }
    };

    p.redrawView = function () {
        if (this.isVisible() && this.renderer) {           
            this.renderer.redraw();
            this._addSliderThumpEvents();
        }
    };

    /**
     * @method redraw
     * Redraws all elements
     */
    p.redraw = function () {
        if (this.isVisible()) {
            this.redrawView();
            this.writeValue();
            this.writeUnit(this.settings.unitSymbol);
        }
    };

    //PRIVAT FUNCTIONS
    p.updateFormat = function () {
        if (this.settings.format !== undefined) {
            var formatObject;
            if (Utils.isObject(this.settings.format)) {
                this.settings.numberFormat = NumberFormat.getFormat(this.settings.format, this.settings.mms);
            } else if (typeof (format) === 'string') {
                if (brease.language.isKey(this.settings.format)) {
                    try {
                        this.setLangDependency(true);
                        var textKey = brease.language.parseKey(this.settings.format);
                        formatObject = JSON.parse(brease.language.getTextByKey(textKey).replace(/'/g, '"'));
                        this.settings.numberFormat = NumberFormat.getFormat(formatObject, this.settings.mms);
                    } catch (error) {
                        console.iatWarn(this.elem.id + ': Format String "' + this.settings.format + '" is invalid!');
                        this.settings.numberFormat = NumberFormat.getFormat({}, this.settings.mms);
                    }
                } else {
                    try {
                        formatObject = JSON.parse(this.settings.format.replace(/'/g, '"'));
                        this.settings.numberFormat = NumberFormat.getFormat(formatObject, this.settings.mms);
                    } catch (error) {
                        console.iatWarn(this.elem.id + ': Format String "' + this.settings.format + '" is invalid!');
                        this.settings.numberFormat = NumberFormat.getFormat({}, this.settings.mms);
                    }
                }
            }
        } else {
            this.settings.numberFormat = NumberFormat.getFormat({}, this.settings.mms);
        }
        this.settings.separators = brease.user.getSeparators();
    };

    function _setUnitSymbol(symbol) {
        this.settings.unitSymbol = symbol;
        this.unitChange.resolve();
    }

    function _updateNodeDisplay(widget) {
        widget.writeUnit(widget.settings.unitSymbol);
        widget.renderer.updateAxis();
        widget.renderer.getCurrentValue();
        widget.writeValue();
    }

    function _unitSettings(widget) {

        widget.settings.mms = brease.measurementSystem.getCurrentMeasurementSystem();
        if (Utils.isObject(widget.settings.unit)) {
            widget.data.node.unit = widget.settings.unit[brease.measurementSystem.getCurrentMeasurementSystem()];
            widget.showUnit();
            widget.setLangDependency(true);
        }
    }

    p.initSettings = function () {
        this.initParse();
        this.setOrientationClasses();
        this.settings.separators = brease.user.getSeparators();
        this.settings.mms = brease.measurementSystem.getCurrentMeasurementSystem();

        this.updateFormat();
        _unitSettings(this);
    };

    p.initParse = function () {
        this.settings.thumbSize = parseInt(this.settings.thumbSize, 10);
        this.settings.valueDisplaySize = parseInt(this.settings.valueDisplaySize, 10);
        this.settings.scaleOffset = parseInt(this.settings.scaleOffset, 10);
        this.settings.trackSize = parseInt(this.settings.trackSize, 10);
    };

    // override method called in BaseWidget.init
    p._initEditor = function () {
        var widget = this;
        require(['widgets/brease/BasicSlider/libs/EditorHandles'], function (EditorHandles) {
            var editorHandles = new EditorHandles(widget);
            widget.getHandles = function () {
                return editorHandles.getHandles();
            };
            widget.designer.getSelectionDecoratables = function () {
                return editorHandles.getSelectionDecoratables();
            };
            widget.dispatchEvent(new CustomEvent(BreaseEvent.WIDGET_EDITOR_IF_READY, { bubbles: true }));
            widget.observer.init();
        });
    };
    function _stopEventPropagation(e) {
        if (!brease.config.editMode && this.isEnabled()) {
            e.stopPropagation();
        }
    }
    function _onThemeChanged() {
        this.redraw(this);
    }
    return contentActivatedDependency.decorate(dragAndDropCapability.decorate(measurementSystemDependency.decorate(languageDependency.decorate(WidgetClass, false), true), false), true);

});
