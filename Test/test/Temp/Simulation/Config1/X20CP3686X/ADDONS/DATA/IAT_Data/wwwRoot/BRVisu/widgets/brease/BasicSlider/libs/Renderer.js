define([
    'brease/core/Class', 
    'brease/events/BreaseEvent', 
    'libs/d3/d3', 
    'brease/core/Types', 
    'brease/enum/Enum', 
    'brease/core/Utils',
    'widgets/brease/common/libs/redux/utils/UtilsSize'
], function (SuperClass, BreaseEvent, d3, Types, Enum, Utils, UtilsSize) {

    'use strict';

    var Renderer = SuperClass.extend(function Renderer(widget) {
            SuperClass.call(this);
            this.widget = widget;
            this.settings = widget.settings;
            this.initialize();
        }, null),

        p = Renderer.prototype;

    p.initialize = function () {
        this.reinitialize();
    };

    p.reinitialize = function () {
        //The following factor is used to give the axis a certain height depending on the widget height
        this.settings.axisSizeFactor = 10;
        this.settings.thumbRadius = (parseInt(this.settings.thumbSize, 10) / 2);
        
        this._setSize();

        this.scaleDomain(this.widget.data.node);
        _calcTickNumbers(this);

        this.initDraw();

        this.valueDisplayDraw();
        this.valueDisplayHandling();
        this.updateThumbPositionByValue(Types.parseValue(this.widget.data.node.value, 'Number', { min: this.widget.data.node.minValue, max: this.widget.data.node.maxValue }));
        this.tickNumbersHandling();
        _imgHandling(this);

        this.enableClassHandling();

    };

    p._setSize = function () {
        this.widgetWidth = UtilsSize.setAndGetWidth(this.settings.width, this.widget.elem);
        this.widgetHeight = UtilsSize.setAndGetHeight(this.settings.height, this.widget.elem);
    };

    p.redraw = function () {
        this.widget.el.children().remove();
        this.reinitialize();
    };

    p.initDraw = function () {

        this.containerId = Utils.uniqueID(this.widget.elem.id + '_container');
        this.axisId = Utils.uniqueID(this.widget.elem.id + '_axis');
        this.containerElement = $("<div class='borderElement' id=" + this.containerId + '>');
        this.widget.el.append(this.containerElement);
        this.containerElement.height(this.widgetHeight)
            .width(this.widgetWidth);

        this.axisElement = d3.select('#' + this.containerId)
            .append('svg')
            .attr('id', this.axisId)
            .attr('class', 'axisElement');

        this.backgroundId = Utils.uniqueID(this.widget.elem.id + '_container');
        this.backgroundElement = $("<div class='backgroundElement' id=" + this.backgroundId + '>');
        this.containerElement.append(this.backgroundElement);

        this.sliderElement = d3.select('#' + this.containerId)
            .append('svg')
            .attr('class', 'sliderElement')
            .attr('width', this.widgetWidth)
            .attr('height', this.widgetHeight);

        this.drawElements();

        this.drawFillElement();
    };

    p.createThumbBackGround = function (orientation, thumbSize, height, width, thumbPosition) {
        var top = 0, left = 0;

        switch (orientation) {
            case Enum.Orientation.LTR: case Enum.Orientation.RTL:
                top = ((height / 2) - (thumbSize / 2));
                left = (thumbPosition - (thumbSize / 2));
                break;
            case Enum.Orientation.TTB: case Enum.Orientation.BTT:
                top = (thumbPosition - (thumbSize / 2));
                left = ((width / 2) - (thumbSize / 2));
                break;
        }

        var thumb = $('<div>')
            .css('height', thumbSize)
            .css('width', thumbSize)
            .css('top', top)
            .css('left', left)
            .attr('class', 'thumbBackground');

        return thumb;
    };

    p.createSliderThumb = function (parent, height, width, thumbSize, dragBehavior, orientation) {
        var el = parent.append('rect')
            .attr('height', thumbSize)
            .attr('width', thumbSize)
            .attr('class', 'sliderThumb')
            .call(dragBehavior);

        switch (orientation) {
            case Enum.Orientation.LTR: case Enum.Orientation.RTL:
                el.attr('y', ((height / 2) - (thumbSize / 2)));
                break;
            case Enum.Orientation.TTB: case Enum.Orientation.BTT:
                el.attr('x', ((width / 2) - (thumbSize / 2)));
                break;
        }

        return el;
    };

    p.createSliderRectangle = function (parent, height, width, trackSize, orientation) {
        var x = 0, y = 0,
            i_height = 0,
            i_width = 0,
            rx = 0, ry = 0,
            el;

        switch (orientation) {
            case Enum.Orientation.LTR: case Enum.Orientation.RTL:
                x = 0;
                y = ((height / 2) - (trackSize / 2));
                i_height = trackSize;
                i_width = width;
                rx = (trackSize / 2);
                ry = 0;
                break;
            case Enum.Orientation.TTB: case Enum.Orientation.BTT:
                x = ((width / 2) - (trackSize / 2));
                y = 0;
                i_height = height;
                i_width = trackSize;
                rx = 0;
                ry = (trackSize / 2);
                break;
        }

        el = parent.append('rect')
            .attr('x', x)
            .attr('y', y)
            .attr('height', i_height)
            .attr('width', i_width)
            .attr('rx', rx)
            .attr('ry', ry)
            .attr('class', 'sliderRectangle');

        return el;
    };

    p.createRectangleBackground = function (orientation, height, width, trackSize) {
        var left = 0, top = 0, i_height = 0, i_width = 0, el;

        switch (orientation) {
            case Enum.Orientation.LTR: case Enum.Orientation.RTL:
                left = 0;
                top = ((height / 2) - (trackSize / 2));
                i_width = width;
                i_height = trackSize;
                break;
            case Enum.Orientation.TTB: case Enum.Orientation.BTT:
                left = ((width / 2) - (trackSize / 2));
                top = 0;
                i_width = trackSize;
                i_height = height;
                break;
        }

        el = $('<div>')
            .css('height', i_height)
            .css('width', i_width)
            .css('top', top)
            .css('left', left)
            .attr('class', 'rectangleBackground');

        return el;
    };

    p.drawElements = function () {

        this.sliderRectangle = this.createSliderRectangle(this.sliderElement, parseInt(this.widgetHeight, 10), parseInt(this.widgetWidth, 10), parseInt(this.settings.trackSize, 10), this.settings.orientation);
        this.createDragBehavior();
        this.sliderThumb = this.createSliderThumb(this.sliderElement, parseInt(this.widgetHeight, 10), parseInt(this.widgetWidth, 10), parseInt(this.settings.thumbSize, 10), this.dragBehavior, this.settings.orientation);
        this.thumbBackground = this.createThumbBackGround(this.settings.orientation, this.settings.thumbSize, parseInt(this.widgetHeight, 10), parseInt(this.widgetWidth, 10), this.thumbScale(this.widget.data.node.value));
        this.rectangleBackground = this.createRectangleBackground(this.settings.orientation, parseInt(this.widgetHeight, 10), parseInt(this.widgetWidth, 10), parseInt(this.settings.trackSize, 10));
        this.backgroundElement.append(this.rectangleBackground).append(this.thumbBackground);

        switch (this.settings.orientation) {
            case Enum.Orientation.LTR: case Enum.Orientation.RTL:
                this.drawHorizontalAxis();
                break;
            case Enum.Orientation.TTB: case Enum.Orientation.BTT:
                this.drawVerticalAxis();
                break;
        }
    };

    p.createDragBehavior = function () {
        this.dragBehavior = d3.behavior.drag();
        if (brease.config.editMode === false) {
            this.widget.el.find('.sliderRectangle').on(BreaseEvent.CLICK, this._bind('_rectangleTrackHandler'));
            this.dragBehavior.on('dragstart', this._bind(this.dragStart));
            this.dragBehavior.on('drag', this._bind(this.updateThumbPositionByEvent));
            this.dragBehavior.on('dragend', this._bind(this.dragEnd));
        }
    };

    p.drawFillElement = function () {
        this.fillElement = $('<div>');
        this.fillElement.addClass('fillElement');
        this.fillElement.addClass(this.settings.orientation);

        switch (this.settings.orientation) {
            case Enum.Orientation.LTR:
                this.fillElement.css('height', this.settings.trackSize);
                this.fillElement.css('width', this.settings.thumbRadius);
                this.fillElement.css('top', (parseFloat(this.widgetHeight, 10) / 2) - (this.settings.trackSize / 2));
                this.fillElement.css('left', 0);
                break;
            case Enum.Orientation.RTL:
                this.fillElement.css('height', this.settings.trackSize);
                this.fillElement.css('width', this.thumbScale(this.settings.value - this.settings.minValue));
                this.fillElement.css('top', (parseFloat(this.widgetHeight, 10) / 2) - (this.settings.trackSize / 2));
                this.fillElement.css('left', this.thumbScale(this.settings.value));
                break;
            case Enum.Orientation.BTT:
                this.fillElement.css('height', this.settings.thumbPosition);
                this.fillElement.css('top', parseFloat(this.widgetHeight, 10) - this.settings.thumbRadius);
                this.fillElement.css('left', (parseFloat(this.widgetWidth, 10) / 2) - this.settings.trackSize / 2);
                this.fillElement.css('width', this.settings.trackSize);
                break;
            case Enum.Orientation.TTB:
                this.fillElement.css('height', this.thumbScale(this.settings.value));
                this.fillElement.css('top', this.thumbScale(this.settings.value));
                this.fillElement.css('left', (parseFloat(this.widgetWidth, 10) / 2) - this.settings.trackSize / 2);
                this.fillElement.css('width', this.settings.trackSize);
                break;
        }
        this.rectangleBackground.after(this.fillElement);
    };

    p.updateFillElement = function () {
        switch (this.settings.orientation) {
            case Enum.Orientation.LTR:
                this.fillElement.css('width', this.settings.thumbPosition);
                this.fillElement.css('left', 0);
                break;
            case Enum.Orientation.RTL:
                this.fillElement.css('width', this.thumbScale(this.settings.minValue) - this.settings.thumbPosition + this.settings.thumbRadius);
                this.fillElement.css('left', this.settings.thumbPosition);
                break;
            case Enum.Orientation.BTT:
                this.fillElement.css('height', this.thumbScale(this.settings.minValue) - this.settings.thumbPosition + this.settings.thumbRadius);
                this.fillElement.css('top', this.settings.thumbPosition);
                break;
            case Enum.Orientation.TTB:
                this.fillElement.css('height', this.settings.thumbPosition);
                this.fillElement.css('top', 0);
                break;
        }
    };

    p.dragStart = function () {
        this.updateThumbPositionByEvent();
    };

    p.dragEnd = function () {
        this.updateThumbPositionByEvent();
    };

    p.updateThumbPositionByEvent = function () {
        d3.event.sourceEvent.preventDefault();
        d3.event.sourceEvent.stopPropagation();
        _smallChangeOffset(this, d3.event);
        this.updateFillElement();
    };

    p.updateThumbPositionByValue = function (value) {
        this.settings.thumbPosition = this.thumbScale(value);
        this.updateThumb();
        this.updateFillElement();
    };

    p.getCurrentValue = function () {
        this.widget.data.node.value = this.thumbScale.invert(this.settings.thumbPosition);
    };

    p.scaleDomain = function (node) {

        if (node !== undefined) {
            this.settings.value = node.value;
            this.settings.minValue = node.minValue;
            this.settings.maxValue = node.maxValue;
        }

        switch (this.settings.orientation) {

            case Enum.Orientation.TTB:
                this.thumbScale = d3.scale.linear()
                    .domain([this.settings.minValue, this.settings.maxValue])
                    .range([this.settings.thumbRadius, parseFloat(this.widgetHeight, 10) - this.settings.thumbRadius]);
                break;

            case Enum.Orientation.BTT:
                this.thumbScale = d3.scale.linear()
                    .domain([this.settings.maxValue, this.settings.minValue])
                    .range([this.settings.thumbRadius, parseFloat(this.widgetHeight, 10) - this.settings.thumbRadius]);
                break;

            case Enum.Orientation.RTL:
                this.thumbScale = d3.scale.linear()
                    .domain([this.settings.maxValue, this.settings.minValue])
                    .range([this.settings.thumbRadius, parseFloat(this.widgetWidth, 10) - this.settings.thumbRadius]);
                break;

            case Enum.Orientation.LTR:
                this.thumbScale = d3.scale.linear()
                    .domain([this.settings.minValue, this.settings.maxValue])
                    .range([this.settings.thumbRadius, parseFloat(this.widgetWidth, 10) - this.settings.thumbRadius]);
                break;
        }

    };

    p.drawHorizontalAxis = function () {

        var tickSize = this.widgetHeight / this.settings.axisSizeFactor;

        if (this.settings.tickPosition === 'top') {
            _appendHorizontalAxisTop(this, tickSize);
        }

        if (this.settings.tickPosition === 'bottom') {
            _appendHorizontalAxisBottom(this, tickSize);
        }

        if (this.settings.tickPosition === 'topbottom') {
            _appendHorizontalAxisTop(this, tickSize);
            _appendHorizontalAxisBottom(this, tickSize);
        }

    };

    p.drawVerticalAxis = function () {

        var tickSize = this.widgetWidth / this.settings.axisSizeFactor;

        if (this.settings.tickPosition === 'top') {
            _appendVerticalAxisTop(this, tickSize);
        }

        if (this.settings.tickPosition === 'bottom') {
            _appendVerticalAxisBottom(this, tickSize);
        }

        if (this.settings.tickPosition === 'topbottom') {
            _appendVerticalAxisTop(this, tickSize);
            _appendVerticalAxisBottom(this, tickSize);
        }

    };

    p._rectangleTrackHandler = function (e) {

        if (this.widget.isDisabled) {
            return;
        }
        
        var largeChange = Types.parseValue(this.settings.largeChange, 'Number', { min: 0, max: 100 }) / 100,
            actValue = this.widget.data.node.value,
            valueRange = this.settings.maxValue - this.settings.minValue,
            newValue;

        var scaleFactor = Utils.getScaleFactor(this.widget.elem);
        //Offset in case of touch
        if (e.offsetX === undefined || e.offsetY === undefined) {
            e.offsetX = (e.pageX - this.widget.elem.getBoundingClientRect().left) / scaleFactor;
            e.offsetY = (e.pageY - this.widget.elem.getBoundingClientRect().top) / scaleFactor;
        }

        switch (this.settings.orientation) {

            case Enum.Orientation.TTB:
                if (e.offsetY < this.settings.thumbPosition) {
                    newValue = Types.parseValue(actValue - valueRange * largeChange, 'Number', { min: this.settings.minValue, max: this.settings.maxValue });
                } else if (e.offsetY > this.settings.thumbPosition) {
                    newValue = Types.parseValue(actValue + valueRange * largeChange, 'Number', { min: this.settings.minValue, max: this.settings.maxValue });
                }
                break;

            case Enum.Orientation.BTT:
                if (e.offsetY < this.settings.thumbPosition) {
                    newValue = Types.parseValue(actValue + valueRange * largeChange, 'Number', { min: this.settings.minValue, max: this.settings.maxValue });
                } else if (e.offsetY > this.settings.thumbPosition) {
                    newValue = Types.parseValue(actValue - valueRange * largeChange, 'Number', { min: this.settings.minValue, max: this.settings.maxValue });
                }
                break;

            case Enum.Orientation.RTL:
                if (e.offsetX < this.settings.thumbPosition) {
                    newValue = Types.parseValue(actValue + valueRange * largeChange, 'Number', { min: this.settings.minValue, max: this.settings.maxValue });
                } else if (e.offsetX > this.settings.thumbPosition) {
                    newValue = Types.parseValue(actValue - valueRange * largeChange, 'Number', { min: this.settings.minValue, max: this.settings.maxValue });
                }
                break;

            case Enum.Orientation.LTR:
                if (e.offsetX < this.settings.thumbPosition) {
                    newValue = Types.parseValue(actValue - valueRange * largeChange, 'Number', { min: this.settings.minValue, max: this.settings.maxValue });
                } else if (e.offsetX > this.settings.thumbPosition) {
                    newValue = Types.parseValue(actValue + valueRange * largeChange, 'Number', { min: this.settings.minValue, max: this.settings.maxValue });
                }
                break;
        }
        if (newValue !== undefined) {
            this.widget.data.node.value = newValue;
            this.updateThumbPositionByValue(newValue);
            this.widget.submitChange();
            this.widget.writeValue();
        }

    };

    p.updateAxis = function () {

        this.settings = this.widget.settings;
        _calcTickNumbers(this);

        var tickSize;

        if (this.settings.orientation === Enum.Orientation.LTR || this.settings.orientation === Enum.Orientation.RTL) {
            tickSize = this.widgetHeight / this.settings.axisSizeFactor;
        } else if (this.settings.orientation === Enum.Orientation.TTB || this.settings.orientation === Enum.Orientation.BTT) {
            tickSize = this.widgetWidth / this.settings.axisSizeFactor;
        }
        if (this.axisAfter !== undefined || this.axisBefore !== undefined) {
            if (this.settings.tickPosition === 'top') {
                this.axisBefore.scale(this.thumbScale)
                    .tickValues(this.tickValues)
                    .tickFormat(d3.format('.' + this.settings.numberFormat.decimalPlaces + 'f'))
                    .innerTickSize(tickSize);

                d3.select('#' + this.axisId).select('.axis-before').call(this.axisBefore);
            }

            if (this.settings.tickPosition === 'bottom') {
                this.axisAfter.scale(this.thumbScale)
                    .tickValues(this.tickValues)
                    .tickFormat(d3.format('.' + this.settings.numberFormat.decimalPlaces + 'f'))
                    .innerTickSize(tickSize);

                d3.select('#' + this.axisId).select('.axis-after').call(this.axisAfter);
            }

            if (this.settings.tickPosition === 'topbottom') {
                this.axisBefore.scale(this.thumbScale)
                    .tickValues(this.tickValues)
                    .tickFormat(d3.format('.' + this.settings.numberFormat.decimalPlaces + 'f'))
                    .innerTickSize(tickSize);

                d3.select('#' + this.axisId).select('.axis-before').call(this.axisBefore);

                this.axisAfter.scale(this.thumbScale)
                    .tickValues(this.tickValues)
                    .tickFormat(d3.format('.' + this.settings.numberFormat.decimalPlaces + 'f'))
                    .innerTickSize(tickSize);

                d3.select('#' + this.axisId).select('.axis-after').call(this.axisAfter);
            }
            this.tickNumbersHandling();
        }

    };

    p.valueDisplayHandling = function () {
        if (this.settings.showValueDisplay) {
            this.output.css('display', '');
        } else {
            this.output.css('display', 'none');
        }
        if (brease.config.editMode === true) {
            this.outputVal.text(this.settings.displayedValue);
            if (this.settings.showUnit) {
                this.outputUnit.text('unit');
            }
        }
    };

    p.tickNumbersHandling = function () {
        if (this.settings.showTickNumbers === true) {
            this.widget.el.find('text').css('display', 'block');
        } else {
            this.widget.el.find('text').css('display', 'none');
        }
    };

    p.tickPositionHandling = function () {
        if (this.axisAfter !== undefined) {
            d3.select('#' + this.axisId).selectAll('.axis-after').remove();
        }
        if (this.axisBefore !== undefined) {
            d3.select('#' + this.axisId).selectAll('.axis-before').remove();
        }

        if (this.settings.orientation === Enum.Orientation.LTR || this.settings.orientation === Enum.Orientation.RTL) {
            this.drawHorizontalAxis();
        } else if (this.settings.orientation === Enum.Orientation.TTB || this.settings.orientation === Enum.Orientation.BTT) {
            this.drawVerticalAxis();
        }
        this.tickNumbersHandling();
    };

    p.updateImg = function (image, appendImgEl) {
        if (appendImgEl) {
            _imgHandling(this);
        } else {
            this.widget.el.find('img').attr('src', image);
        }
    };

    p.thumbCornerHandling = function (thumbImage) {
        if (thumbImage) {
            this.sliderThumb.attr('rx', 0);
        } else {
            this.sliderThumb.attr('rx', (this.settings.thumbSize / 2));
        }
    };

    p.valueDisplayDraw = function () {

        var outputPadding = 8,
            width = 'auto';

        this.output = $('<output/>')
            .attr('class', 'valueDisplay');

        this.outputVal = $('<span/>')
            .attr('class', 'valueOutput');

        this.outputUnit = $('<span/>')
            .attr('class', 'unitOutput');

        this.outputArrow = $('<span/>')
            .attr('class', 'arrowOutput');

        this.output.append([this.outputVal, this.outputUnit, this.outputArrow]);
        this.containerElement.append(this.output);

        if (this.settings.ellipsis === true) {
            width = this.settings.valueDisplaySize;
            this.output.addClass('ellipsis');
        } else {
            width = 'auto';
            this.output.removeClass('ellipsis');
        }

        if (this.settings.orientation === Enum.Orientation.LTR || this.settings.orientation === Enum.Orientation.RTL) {
            this.output.addClass('horizontal');
            this.output.removeClass('vertical');
            this.output.css('height', this.settings.valueDisplaySize)
                .css('top', this.widgetHeight / 2 - this.settings.valueDisplaySize - this.settings.thumbRadius - outputPadding + parseInt(this.widget.el.css('padding-top'), 10))
                .css('width', width);
        } else if (this.settings.orientation === Enum.Orientation.TTB || this.settings.orientation === Enum.Orientation.BTT) {
            this.output.addClass('vertical');
            this.output.removeClass('horizontal');
            this.output.css('width', width)
                .css('right', this.widgetWidth / 2 + this.settings.thumbRadius + outputPadding + parseInt(this.widget.el.css('padding-left'), 10))
                .css('height', this.settings.valueDisplaySize);
        }
    };

    p.dispose = function () {
        this.dragBehavior.on('drag', null);
        this.widget.el.find('.sliderRectangle').off(BreaseEvent.CLICK, this._bind('_rectangleTrackHandler'));
        SuperClass.prototype.dispose.call(this);
    };

    p.updateThumb = function () {

        if (this.settings.orientation === Enum.Orientation.LTR || this.settings.orientation === Enum.Orientation.RTL) {
            this.sliderThumb.attr('x', this.settings.thumbPosition - this.settings.thumbRadius);
            this.thumbBackground.css('left', this.settings.thumbPosition - this.settings.thumbRadius);
            this.output.css('left', this.settings.thumbPosition + parseInt(this.widget.el.css('padding-left'), 10));
        } else if (this.settings.orientation === Enum.Orientation.TTB || this.settings.orientation === Enum.Orientation.BTT) {
            this.sliderThumb.attr('y', this.settings.thumbPosition - this.settings.thumbRadius);
            this.thumbBackground.css('top', this.settings.thumbPosition - this.settings.thumbRadius);
            this.output.css('top', this.settings.thumbPosition + parseInt(this.widget.el.css('padding-top'), 10));
        }
    };

    p.enableClassHandling = function () {
        if (this.widget.isDisabled) {
            this.rectangleBackground.addClass('disabled');
            this.thumbBackground.addClass('disabled');
        } else {
            this.rectangleBackground.removeClass('disabled');
            this.thumbBackground.removeClass('disabled');
        }
    };

    //PRIVATE FUNCTIONS
    function _appendVerticalAxisTop(renderer, tickSize) {
        renderer.axisBefore = d3.svg.axis()
            .scale(renderer.thumbScale)
            .tickValues(renderer.tickValues)
            .innerTickSize(tickSize)
            .tickFormat(d3.format('.' + renderer.settings.numberFormat.decimalPlaces + 'f'))
            .orient('left');

        renderer.axisElement.append('g')
            .call(renderer.axisBefore)
            .attr('class', 'axis-before')
            .attr('transform', 'translate(' + (renderer.widgetWidth / 2 - renderer.settings.scaleOffset) + ',0)');
    }

    function _appendVerticalAxisBottom(renderer, tickSize) {
        renderer.axisAfter = d3.svg.axis()
            .scale(renderer.thumbScale)
            .tickValues(renderer.tickValues)
            .innerTickSize(tickSize)
            .tickFormat(d3.format('.' + renderer.settings.numberFormat.decimalPlaces + 'f'))
            .orient('right');

        renderer.axisElement.append('g')
            .call(renderer.axisAfter)
            .attr('class', 'axis-after')
            .attr('transform', 'translate(' + (renderer.widgetWidth / 2 + renderer.settings.scaleOffset) + ',0)');
    }

    function _appendHorizontalAxisTop(renderer, tickSize) {
        renderer.axisBefore = d3.svg.axis()
            .scale(renderer.thumbScale)
            .tickValues(renderer.tickValues)
            .innerTickSize(tickSize)
            .tickFormat(d3.format('.' + renderer.settings.numberFormat.decimalPlaces + 'f'))
            .orient('top');

        renderer.axisElement.append('g')
            .call(renderer.axisBefore)
            .attr('class', 'axis-before')
            .attr('transform', 'translate(0,' + (renderer.widgetHeight / 2 - renderer.settings.scaleOffset) + ')');
    }

    function _appendHorizontalAxisBottom(renderer, tickSize) {
        renderer.axisAfter = d3.svg.axis()
            .scale(renderer.thumbScale)
            .tickValues(renderer.tickValues)
            .innerTickSize(tickSize)
            .tickFormat(d3.format('.' + renderer.settings.numberFormat.decimalPlaces + 'f'))
            .orient('bottom');

        renderer.axisElement.append('g')
            .call(renderer.axisAfter)
            .attr('class', 'axis-after')
            .attr('transform', 'translate(0,' + (renderer.widgetHeight / 2 + renderer.settings.scaleOffset) + ')');
    }

    function _smallChangeOffset(renderer, position) {

        if (renderer.widget.isDisabled) {
            return;
        }

        var smallChange = Types.parseValue(renderer.settings.smallChange, 'Number', { min: 0, max: 100 }) / 100;
        var offset = (renderer.thumbScale(renderer.settings.maxValue) - renderer.thumbScale(renderer.settings.minValue)) * smallChange,
            valueChange;

        if (!renderer.scaleFactor) {
            renderer.scaleFactor = Utils.getChromeScale(renderer.widget.elem);
        }

        switch (renderer.settings.orientation) {
            case Enum.Orientation.LTR:
                if (position.x / renderer.scaleFactor >= renderer.settings.thumbPosition + offset) {
                    valueChange = renderer.widget.data.node.value + (renderer.settings.maxValue - renderer.settings.minValue) * smallChange * _numberOfSmallChanges(renderer, position, offset);
                    renderer.settings.thumbPosition = Types.parseValue(renderer.thumbScale(valueChange), 'Number', { min: renderer.thumbScale(renderer.settings.minValue), max: renderer.thumbScale(renderer.settings.maxValue) });
                }

                if (position.x / renderer.scaleFactor <= renderer.settings.thumbPosition - offset) {
                    valueChange = renderer.widget.data.node.value - (renderer.settings.maxValue - renderer.settings.minValue) * smallChange * _numberOfSmallChanges(renderer, position, offset);
                    renderer.settings.thumbPosition = Types.parseValue(renderer.thumbScale(valueChange), 'Number', { min: renderer.thumbScale(renderer.settings.minValue), max: renderer.thumbScale(renderer.settings.maxValue) });
                }
                break;

            case Enum.Orientation.RTL:
                if (position.x / renderer.scaleFactor <= renderer.settings.thumbPosition + offset) {
                    valueChange = renderer.widget.data.node.value - (renderer.settings.maxValue - renderer.settings.minValue) * smallChange * _numberOfSmallChanges(renderer, position, offset);
                    renderer.settings.thumbPosition = Types.parseValue(renderer.thumbScale(valueChange), 'Number', { min: renderer.thumbScale(renderer.settings.maxValue), max: renderer.thumbScale(renderer.settings.minValue) });
                }

                if (position.x / renderer.scaleFactor >= renderer.settings.thumbPosition - offset) {
                    valueChange = renderer.widget.data.node.value + (renderer.settings.maxValue - renderer.settings.minValue) * smallChange * _numberOfSmallChanges(renderer, position, offset);
                    renderer.settings.thumbPosition = Types.parseValue(renderer.thumbScale(valueChange), 'Number', { min: renderer.thumbScale(renderer.settings.maxValue), max: renderer.thumbScale(renderer.settings.minValue) });
                }
                break;

            case Enum.Orientation.TTB:
                if (position.y / renderer.scaleFactor >= renderer.settings.thumbPosition + offset) {
                    valueChange = renderer.widget.data.node.value + (renderer.settings.maxValue - renderer.settings.minValue) * smallChange * _numberOfSmallChanges(renderer, position, offset);
                    renderer.settings.thumbPosition = Types.parseValue(renderer.thumbScale(valueChange), 'Number', { min: renderer.thumbScale(renderer.settings.minValue), max: renderer.thumbScale(renderer.settings.maxValue) });
                }

                if (position.y / renderer.scaleFactor <= renderer.settings.thumbPosition - offset) {
                    valueChange = renderer.widget.data.node.value - (renderer.settings.maxValue - renderer.settings.minValue) * smallChange * _numberOfSmallChanges(renderer, position, offset);
                    renderer.settings.thumbPosition = Types.parseValue(renderer.thumbScale(valueChange), 'Number', { min: renderer.thumbScale(renderer.settings.minValue), max: renderer.thumbScale(renderer.settings.maxValue) });
                }
                break;

            case Enum.Orientation.BTT:
                if (position.y / renderer.scaleFactor <= renderer.settings.thumbPosition + offset) {
                    valueChange = renderer.widget.data.node.value - (renderer.settings.maxValue - renderer.settings.minValue) * smallChange * _numberOfSmallChanges(renderer, position, offset);
                    renderer.settings.thumbPosition = Types.parseValue(renderer.thumbScale(valueChange), 'Number', { min: renderer.thumbScale(renderer.settings.maxValue), max: renderer.thumbScale(renderer.settings.minValue) });
                }

                if (position.y / renderer.scaleFactor >= renderer.settings.thumbPosition - offset) {
                    valueChange = renderer.widget.data.node.value + (renderer.settings.maxValue - renderer.settings.minValue) * smallChange * _numberOfSmallChanges(renderer, position, offset);
                    renderer.settings.thumbPosition = Types.parseValue(renderer.thumbScale(valueChange), 'Number', { min: renderer.thumbScale(renderer.settings.maxValue), max: renderer.thumbScale(renderer.settings.minValue) });
                }
                break;
        }
        renderer.updateThumb();
    }

    function _numberOfSmallChanges(renderer, position, offset) {
        var steps;

        if (renderer.settings.orientation === Enum.Orientation.LTR || renderer.settings.orientation === Enum.Orientation.RTL) {
            steps = Math.floor(Math.abs(renderer.settings.thumbPosition - position.x / renderer.scaleFactor) / offset);
        } else if (renderer.settings.orientation === Enum.Orientation.TTB || renderer.settings.orientation === Enum.Orientation.BTT) {
            steps = Math.floor(Math.abs(renderer.settings.thumbPosition - position.y / renderer.scaleFactor) / offset);
        }
        return steps;
    }

    function _calcTickNumbers(renderer) {

        var steps = (renderer.settings.maxValue - renderer.settings.minValue) / (renderer.settings.majorTicks + 1);
        renderer.tickValues = [];

        for (var i = 0; i < renderer.settings.majorTicks + 2; i += 1) {
            renderer.tickValues[i] = parseFloat(brease.formatter.formatNumber((renderer.settings.minValue + i * steps), renderer.settings.numberFormat, renderer.settings.useDigitGrouping, renderer.settings.separators));
        }
    }

    function _imgHandling(renderer) {
        if (renderer.settings.image !== undefined && renderer.settings.image !== '') {
            var imgElement = '<img src=' + renderer.settings.image + '></img>';
            renderer.thumbBackground.append(imgElement);
            renderer.thumbCornerHandling(true);
        } else {
            renderer.thumbCornerHandling(false);
        }
    }

    return Renderer;

});
