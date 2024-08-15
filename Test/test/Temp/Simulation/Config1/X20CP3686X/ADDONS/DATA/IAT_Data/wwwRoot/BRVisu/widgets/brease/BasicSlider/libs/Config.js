define([
    'brease/enum/Enum'
], function (Enum) {

    'use strict';

    /**
     * @class widgets.brease.BasicSlider.Config
     * @extends core.javascript.Object
     * @override widgets.brease.BasicSlider
     */

    /**
     * @cfg {Boolean} changeOnMove=true
     * @iatStudioExposed
     * @iatCategory Behavior
     * Send value to plc during Movement of the slider if true.
     */

    /**
     * @cfg {brease.config.MeasurementSystemFormat} format= {'metric':{ 'decimalPlaces' : 1, 'minimumIntegerDigits' : 1 }, 'imperial' :{ 'decimalPlaces' : 1, 'minimumIntegerDigits' : 1 }, 'imperial-us' :{ 'decimalPlaces' : 1, 'minimumIntegerDigits' : 1 }} 
     * @iatStudioExposed
     * @iatCategory Appearance
     * @bindable
     * NumberFormat for every measurement system
     */

    /**
     * @cfg {UNumber} largeChange=10
     * @iatCategory Behavior
     * @iatStudioExposed
     * Step interval of the slider.  
     */

    /**
     * @cfg {UInteger} majorTicks=5
     * @iatStudioExposed
     * @iatCategory Appearance
     * Number of displayed ticks of the slider.  
     */

    /**
     * @cfg {Number} maxValue=100
     * @iatStudioExposed
     * @iatCategory Behavior
     * Maximal displayable value.
     */

    /**
     * @cfg {Number} minValue=0
     * @iatStudioExposed
     * @iatCategory Behavior
     * Minimal displayable value.
     */

    /**
     * @cfg {brease.datatype.Node} node=''
     * @iatStudioExposed
     * @iatCategory Data
     * @bindable
     * @editableBinding
     * Value with unit for node binding.  
     */

    /**
     * @cfg {brease.enum.Orientation} orientation='LeftToRight'
     * @iatStudioExposed
     * @iatCategory Appearance
     * Orientation of the slider.  
     */

    /**
     * @cfg {Boolean} showTickNumbers=false
     * @iatStudioExposed
     * @iatCategory Appearance
     * Option to show the numbers next to the ticks.  
     */

    /**
     * @cfg {Boolean} showUnit=false
     * @iatStudioExposed
     * @iatCategory Behavior
     * Determines if the unit is shown in the value display.  
     */

    /**
     * @cfg {Boolean} showValueDisplay=false
     * @iatStudioExposed
     * @iatCategory Behavior
     * Option to show value and unit in an output next to the slider thumb.
     */

    /**
     * @cfg {UNumber} smallChange=1
     * @iatStudioExposed
     * @iatCategory Behavior
     * Step interval of the slider by dragging the thumb.
     */

    /**
     * @cfg {brease.enum.TickStyle} tickPosition='none'
     * @iatStudioExposed
     * @iatCategory Appearance
     * Position of the axis with the ticks.  
     */

    /**
     * @cfg {brease.config.MeasurementSystemUnit} unit=''
     * @bindable
     * @iatStudioExposed
     * @iatCategory Appearance
     * Unit code for every measurement system.  
     */

    /**
     * @cfg {Number} value=0
     * @bindable
     * @nodeRefId node
     * @iatStudioExposed
     * @iatCategory Data
     * @editableBinding
     * Initial visible value of slider.
     */

    /**
     * @cfg {PixelVal} thumbSize='25px'
     * @iatStudioExposed
     * @iatCategory Appearance
     * Specifies the size of the slider thumb in pixel.
     */

    /**
     * @cfg {PixelVal} scaleOffset='15px'
     * @iatStudioExposed
     * @iatCategory Appearance
     * Specifies the padding between the scale and the track of the widget in pixel.  
     */

    /**
     * @cfg {PixelVal} valueDisplaySize='35px'
     * @iatStudioExposed
     * @iatCategory Appearance
     * Size of the value display in pixel.  
     */

    /**
     * @cfg {PixelVal} trackSize='7px'
     * @iatStudioExposed
     * @iatCategory Appearance
     * Thickness of the slider track in pixel.  
     */

    /**
     * @cfg {ImagePath} image=''
     * @iatStudioExposed
     * @iatCategory Appearance
     * Path to an optional image for the slider thumb.  
     */

    /**
     * @cfg {Boolean} ellipsis=false
     * @iatStudioExposed
     * @iatCategory Appearance
     * Defines if the Value inside the Valuedisplay should be shown with ellipsis.  
     */

    return {
        changeOnMove: true,
        largeChange: 10,
        majorTicks: 5,
        maxValue: 100,
        minValue: 0,
        orientation: Enum.Orientation.LTR,
        smallChange: 1,
        showTickNumbers: false,
        showValueDisplay: false,
        tickPosition: Enum.TickStyle.NONE,
        value: 0,
        thumbSize: '25px',
        scaleOffset: '15px',
        valueDisplaySize: '35px',
        trackSize: '7px',
        image: '',
        ellipsis: false,
        showUnit: false
    };
});
