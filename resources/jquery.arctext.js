/**
 * Arctext.js
 * A jQuery plugin for curved text
 * http://www.codrops.com
 *
 * Copyright 2011, Pedro Botelho / Codrops
 * Free to use under the MIT license.
 *
 * Date: Mon Jan 23 2012
 */

(function( $, undefined ) {

	/*!
	* FitText.js 1.2
	*
	* Copyright 2011, Dave Rupert http://daverupert.com
	* Released under the WTFPL license
	* http://sam.zoy.org/wtfpl/
	*
	* Date: Thu May 05 14:23:00 2011 -0600
	*/
	$.fn.fitText = function( kompressor, options ) {

		// Setup options
		var compressor = kompressor || 1,
			settings = $.extend({
				'minFontSize' : Number.NEGATIVE_INFINITY,
				'maxFontSize' : Number.POSITIVE_INFINITY
			}, options);

		return this.each(function(){

			// Store the object
			var $this = $(this);

			// Resizer() resizes items based on the object width divided by the compressor * 10
			var resizer = function () {
				$this.css('font-size', Math.max(Math.min($this.width() / (compressor*10), parseFloat(settings.maxFontSize)), parseFloat(settings.minFontSize)));
			};

			// Call once to set.
			resizer();

			// Call on resize. Opera debounces their resize by default.
			$(window).on('resize.fittext orientationchange.fittext', resizer);

		});

	};

	/*
	 * Lettering plugin
	 *
	 * changed injector function:
	 *   add &nbsp; for empty chars.
	 */
	function injector(t, splitter, klass, after) {
		var a = t.text().split(splitter), inject = '', emptyclass;
		if (a.length) {
			$(a).each(function(i, item) {
				emptyclass = '';
				if(item === ' ') {
					emptyclass = ' empty';
					item='&nbsp;';
				}
				inject += '<span class="'+klass+(i+1)+emptyclass+'">'+item+'</span>'+after;
			});
			t.empty().append(inject);
		}
	}

	var methods 			= {
		init : function() {

			return this.each(function() {
				injector($(this), '', 'char', '');
			});

		},

		words : function() {

			return this.each(function() {
				injector($(this), ' ', 'word', ' ');
			});

		},

		lines : function() {

			return this.each(function() {
				var r = "eefec303079ad17405c889e092e105b0";
				// Because it's hard to split a <br/> tag consistently across browsers,
				// (*ahem* IE *ahem*), we replaces all <br/> instances with an md5 hash
				// (of the word "split").  If you're trying to use this plugin on that
				// md5 hash string, it will fail because you're being ridiculous.
				injector($(this).children("br").replaceWith(r).end(), r, 'line', '');
			});

		}
	};

	$.fn.lettering 			= function( method ) {
		// Method calling logic
		if ( method && methods[method] ) {
			return methods[ method ].apply( this, [].slice.call( arguments, 1 ));
		} else if ( method === 'letters' || ! method ) {
			return methods.init.apply( this, [].slice.call( arguments, 0 ) ); // always pass an array
		}
		$.error( 'Method ' +  method + ' does not exist on jQuery.lettering' );
		return this;
	};

	/*
	 * Arctext object.
	 */
	$.Arctext 				= function( options, element ) {

		this.$el	= $( element );
		this._init( options );

	};

	$.Arctext.defaults 		= {
		radius	: 0, 	// the minimum value allowed is half of the word length. if set to -1, the word will be straight.
		dir		: 1,	// 1: curve is down, -1: curve is up.
		rotate	: true,	// if true each letter will be rotated.
		fitText	: false // if you wanna try out the fitText plugin (http://fittextjs.com/) set this to true. Don't forget the wrapper should be fluid.
    };

	$.Arctext.prototype 	= {
		_init 				: function( options ) {

			this.options 		= $.extend( true, {}, $.Arctext.defaults, options );

			// apply the lettering plugin.
			this._applyLettering();

			this.$el.data( 'arctext', true );

			// calculate values
			this._calc();

			// apply transformation.
			this._rotateWord();

			// load the events
			this._loadEvents();

		},
		_applyLettering		: function() {

			this.$el.lettering();

			if( this.options.fitText )
				this.$el.fitText();

			this.$letters	= this.$el.find('span').css('display', 'inline-block');

		},
		_calc				: function() {

			if( this.options.radius === -1 )
				return false;

			// calculate word / arc sizes & distances.
			this._calcBase();

			// get final values for each letter.
			this._calcLetters();

		},
		_calcBase			: function() {

			// total word width (sum of letters widths)
			this.dtWord		= 0;

			var _self 		= this;

			this.$letters.each( function(i) {

				var $letter 		= $(this),
					letterWidth		= $letter.outerWidth( true );

				_self.dtWord += letterWidth;

				// save the center point of each letter:
				$letter.data( 'center', _self.dtWord - letterWidth / 2 );

			});

			// the middle point of the word.
			var centerWord = this.dtWord / 2;

			// check radius : the minimum value allowed is half of the word length.
			if( this.options.radius < centerWord )
				this.options.radius = centerWord;

			// total arc segment length, where the letters will be placed.
			this.dtArcBase	= this.dtWord;

			// calculate the arc (length) that goes from the beginning of the first letter (x=0) to the end of the last letter (x=this.dtWord).
			// first lets calculate the angle for the triangle with base = this.dtArcBase and the other two sides = radius.
			var angle		= 2 * Math.asin( this.dtArcBase / ( 2 * this.options.radius ) );

			// given the formula: L(ength) = R(adius) x A(ngle), we calculate our arc length.
			this.dtArc		= this.options.radius * angle;

		},
		_calcLetters		: function() {

			var _self 		= this,
				iteratorX 	= 0;

			this.$letters.each( function(i) {

				var $letter 		= $(this),
					// calculate each letter's semi arc given the percentage of each letter on the original word.
					dtArcLetter		= ( $letter.outerWidth( true ) / _self.dtWord ) * _self.dtArc,
					// angle for the dtArcLetter given our radius.
					beta			= dtArcLetter / _self.options.radius,
					// distance from the middle point of the semi arc's chord to the center of the circle.
					// this is going to be the place where the letter will be positioned.
					h				= _self.options.radius * ( Math.cos( beta / 2 ) ),
					// angle formed by the x-axis and the left most point of the chord.
					alpha			= Math.acos( ( _self.dtWord / 2 - iteratorX ) / _self.options.radius ),
					// angle formed by the x-axis and the right most point of the chord.
					theta 			= alpha + beta / 2,
					// distances of the sides of the triangle formed by h and the orthogonal to the x-axis.
					x				= Math.cos( theta ) * h,
					y				= Math.sin( theta ) * h,
					// the value for the coordinate x of the middle point of the chord.
					xpos			= iteratorX + Math.abs( _self.dtWord / 2 - x - iteratorX ),
					// finally, calculate how much to translate each letter, given its center point.
					// also calculate the angle to rotate the letter accordingly.
					xval	= 0| xpos - $letter.data( 'center' ),
					yval	= 0| _self.options.radius - y,
					angle 	= ( _self.options.rotate ) ? 0| -Math.asin( x / _self.options.radius ) * ( 180 / Math.PI ) : 0;

				// the iteratorX will be positioned on the second point of each semi arc
				iteratorX = 2 * xpos - iteratorX;

				// save these values
				$letter.data({
					x	: xval,
					y	: ( _self.options.dir === 1 ) ? yval : -yval,
					a	: ( _self.options.dir === 1 ) ? angle : -angle
				});

			});

		},
		_rotateWord			: function( animation ) {

			if( !this.$el.data('arctext') ) return false;

			var _self = this;

			this.$letters.each( function(i) {

				var $letter 		= $(this),
					transformation	= ( _self.options.radius === -1 ) ? 'none' : 'translateX(' + $letter.data('x') + 'px) translateY(' + $letter.data('y') + 'px) rotate(' + $letter.data('a') + 'deg)',
					transition		= ( animation ) ? 'all ' + ( animation.speed || 0 ) + 'ms ' + ( animation.easing || 'linear' ) : 'none';

				$letter.css({
					'-webkit-transition' : transition,
					'-moz-transition' : transition,
					'-o-transition' : transition,
					'-ms-transition' : transition,
					'transition' : transition
				})
				.css({
					'-webkit-transform' : transformation,
					'-moz-transform' : transformation,
					'-o-transform' : transformation,
					'-ms-transform' : transformation,
					'transform' : transformation
				});

			});

		},
		_loadEvents			: function() {

			if( this.options.fitText ) {

				var _self = this;

				$(window).on( 'resize.arctext', function() {

					_self._calc();

					// apply transformation.
					_self._rotateWord();

				});

			}

		},
		set					: function( opts ) {

			if( !opts.radius &&
				!opts.dir &&
				opts.rotate === 'undefined' ) {
					return false;
			}

			this.options.radius = opts.radius || this.options.radius;
			this.options.dir 	= opts.dir || this.options.dir;

			if( opts.rotate !== undefined ) {
				this.options.rotate = opts.rotate;
			}

			this._calc();

			this._rotateWord( opts.animation );

		},
		destroy				: function() {

			this.options.radius	= -1;
			this._rotateWord();
			this.$letters.removeData('x y a center');
			this.$el.removeData('arctext');
			$(window).off('.arctext');

		}
	};

	var logError 			= function( message ) {
		if ( this.console ) {
			console.error( message );
		}
	};

	$.fn.arctext			= function( options ) {

		if ( typeof options === 'string' ) {

			var args = Array.prototype.slice.call( arguments, 1 );

			this.each(function() {

				var instance = $.data( this, 'arctext' );

				if ( !instance ) {
					logError( "cannot call methods on arctext prior to initialization; " +
					"attempted to call method '" + options + "'" );
					return;
				}

				if ( !$.isFunction( instance[options] ) || options.charAt(0) === "_" ) {
					logError( "no such method '" + options + "' for arctext instance" );
					return;
				}

				instance[ options ].apply( instance, args );

			});

		}
		else {

			this.each(function() {

				var instance = $.data( this, 'arctext' );
				if ( !instance ) {
					$.data( this, 'arctext', new $.Arctext( options, this ) );
				}
			});

		}

		return this;

	};

})( jQuery );