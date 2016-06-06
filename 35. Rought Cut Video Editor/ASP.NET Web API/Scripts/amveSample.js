function initPlayer() {
    function onClipdataCallback(clipData) {
        if (clipData) {
            ////for debug
            //var dataTxt = 'data.src: ' + clipData.src +
            //    '\ndata.markin: ' + clipData.markIn +
            //    '\ndata.markout: ' + clipData.markOut +
            //    '\ndata.title: ' + clipData.title +
            //    '\ndata.description: ' + clipData.description +
            //    '\ndata.thumbnail: ' + (clipData.thumbnail ? clipData.thumbnail.dataUrl : null);
            //alert(dataTxt);

            var uri = 'api/Encoder';
           
            $(document).ready(function () {
                
                $.blockUI({
                    message: '<h2>Azure上でトランスコードしています。<H2><be /><h3>数分お待ちください...</h3>',
                    fadeIn: 5000,
                    fadeOut: 2000,
                    css: {
                        border: 'none',
                        padding: '10px',
                        backgroundColor: '#333',
                        opacity: .5,
                        color: '#fff'
                    },
                    overlayCSS: {
                        backgroundColor: '#00f',
                        opacity: 0.6
                    }

                });

                myPlayer.pause();

                $.get(uri, {
                    StartTime: clipData.markIn,
                    EndTime: clipData.markOut,
                    Title: clipData.title,
                    Description: clipData.description,
                    Source: clipData.src
                })
                .done(function (data) {
                    if (data) {
                        myPlayerCliped.src([{ src: data, type: 'application/dash+xml' }]);
                        $('#ampCliped')[0].style.display = 'block';
                        $.unblockUI();
                        myPlayerCliped.play();
                    }
                })
                ;
                
                
        });

        }
    }

    var myOptions = {
        'nativeControlsForTouch': false,
        autoplay: true,
        controls: true,
        poster: '',
        plugins: {
            AMVE: {
                containerId: 'amve',
                clipdataCallback: onClipdataCallback
            }
        }
    };

    var myPlayer = amp('vid1', myOptions);
    $('#amve')[0].style.display = 'none';

    var myOptionsCliped = {
        'nativeControlsForTouch': false,
        autoplay: true,
        controls: true,
        height: 610,
        width: 1080,
        poster: ''
    };

    var myPlayerCliped = amp('vid2', myOptionsCliped);
    $('#ampCliped')[0].style.display = 'none';

    $('#urlEntrySubmitBtn').click(function () {
        var url = undefined;
        var urlEntry = document.getElementById('urlEntryTxt');
        if (urlEntry && urlEntry.value && urlEntry.value.length > 0 && urlEntry.value.indexOf('http') === 0) {
            url = urlEntry.value;
        }

        if (url) {
            myPlayer.src([{ src: url, type: 'application/dash+xml' } ]);
            $('#amve')[0].style.display = 'block';
        }
    });
}