function initPlayer() {

    //Adobe Premier Pro keyboard shorcut config
    var keyboadShortcutConfig = new AMVE.AdobePremierProShortcutConfig();

    //Avid keyboard shorcut config
    //keyboadShortcutConfig = new AMVE.AvidShortcutConfig();

    // Custom keyboard shorcut config
    keyboadShortcutConfig = new AMVE.KeyboardShortcutConfig();
    keyboadShortcutConfig.playPauseShortcut = 'space';
    keyboadShortcutConfig.markInShortcut = 'i';
    keyboadShortcutConfig.markOutShortcut = 'o';
    keyboadShortcutConfig.goToInShortcut = 'shift+i';
    keyboadShortcutConfig.goToOutShortcut = 'shift+o';
    keyboadShortcutConfig.clearInShortcut = 'ctrl+shift+i';
    keyboadShortcutConfig.clearOutShortcut = 'ctrl+shift+o';
    keyboadShortcutConfig.clearBothShortcut = 'ctrl+shift+x';
    keyboadShortcutConfig.playInToOutShortcut = 'ctrl+shift+space';
    keyboadShortcutConfig.playToOutShortcut = 'ctrl+space';
    keyboadShortcutConfig.ffwdShortcut = 'ctrl+right';
    keyboadShortcutConfig.rwdShortcut = 'ctrl+left';
    keyboadShortcutConfig.backOneFrameShortcut = 'left';
    keyboadShortcutConfig.backFiveFramesShortcut = 'shift+left';
    keyboadShortcutConfig.backOneSecondShortcut = 'alt+shift+left';
    keyboadShortcutConfig.backOneGopShortcut = 'ctrl+shift+left';
    keyboadShortcutConfig.fwdOneFrameShortcut = 'right';
    keyboadShortcutConfig.fwdFiveFramesShortcut = 'shift+right';
    keyboadShortcutConfig.fwdOneSecondShortcut = 'alt+shift+right';
    keyboadShortcutConfig.fwdOneGopShortcut = 'ctrl+shift+right';
    keyboadShortcutConfig.markInOneFrameShortcut = 'alt+,';
    keyboadShortcutConfig.markInFiveFramesShortcut = 'alt+shift+,';
    keyboadShortcutConfig.markOutOneFrameShortcut = 'alt+.';
    keyboadShortcutConfig.markOutFiveFramesShortcut = 'alt+shift+.';
    keyboadShortcutConfig.undoShortcut = 'ctrl+z';
    keyboadShortcutConfig.redoShortcut = 'ctrl+y';
    keyboadShortcutConfig.exportShortcut = 's';
    keyboadShortcutConfig.changeModeVirtualShortcut = 'shift+v';
    keyboadShortcutConfig.changeModeRenderedShortcut = 'shift+r';
    keyboadShortcutConfig.changeModeTrimShortcut = 'shift+t';

    function formatTime(time) {
        var returnVal = '';
        if(time >= 0) {
            var d = Math.floor(time / 86400);
            time -= (d * 86400);
            var h = Math.floor(time / 3600) % 24;
            time -= (h * 3600);
            var m = Math.floor(time / 60) % 60;
            time -= (m * 60);
            var s = Math.floor(time);
            time -= s;
            var hs = Math.floor(time * 100);

            returnVal = returnVal + (d > 0) ? d + '.' : '';
            returnVal = returnVal + (h < 10 ? ('0' + h) : ('' + h));
            returnVal = returnVal + ':' + (m < 10 ? ('0' + m) : ('' + m));
            returnVal = returnVal + ':' + (s < 10 ? ('0' + s) : ('' + s));
            if (hs > 0) {
                returnVal = returnVal + '.' + hs;
            }
        }

        return returnVal;
    }


    function onClipdataCallback(clipData) {
        if (clipData) {
            //var dataTxt = 'data.src: ' + clipData.src +
            //    '\ndata.markin: ' + formatTime(clipData.markIn) +
            //    '\ndata.markout: ' + formatTime(clipData.markOut) +
            //    '\ndata.clipDuration: ' + formatTime(clipData.clipDuration) +
            //    '\ndata.title: ' + clipData.title +
            //    '\ndata.description: ' + clipData.description +
            //    '\ndata.thumbnail: ' + (clipData.thumbnail ? clipData.thumbnail.dataUrl : null);
            //alert(dataTxt);

            var uri = 'api/Encoder';
           
            $(document).ready(function () {
                
                $.blockUI({
                    message: '<h2>Azure上でトランスコードしています。<H2><be /><h3>10分弱程度かかります。少々お待ちください...</h3>',
                    fadeIn: 3000,
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
        flashSS: {
            swf: 'tech-wrappers/Players/osmf/StrobeMediaPlayback.2.0.swf',
            plugin: 'tech-wrappers/Players/osmf/MSAdaptiveStreamingPlugin-osmf2.0.swf'
        },
        silverlightSS: {
            xap: 'tech-wrappers/Players/smf/SmoothStreamingPlayer.xap'
        },
        plugins: {
            AMVE: { containerId: 'amve', customMetadataContainerId: 'custommetadata', clipdataCallback: onClipdataCallback, keyboardShortcutConfig: keyboadShortcutConfig }
        }
    };

    var myPlayer = amp('vid1', myOptions);
    $('#amve')[0].style.display = 'none';
    var verInfo = $('#versionInfoDisplay')[0];
    verInfo.innerHTML = 'v. ' + AMVE.AMVEVersionInfo.getVersionInfo();

    var myOptionsCliped = {
        'nativeControlsForTouch': false,
        autoplay: true,
        controls: true,
        poster: ''
    };

    var myPlayerCliped = amp('vid2', myOptionsCliped);
    $('#ampCliped')[0].style.display = 'none';

    $('#urlEntrySubmitBtn').click(function () {
        var url = undefined;
        var urlEntry = document.getElementById('urlEntryTxt');
        if (urlEntry && urlEntry.value && urlEntry.value.length > 0 && urlEntry.value.indexOf('http') === 0) {
            url = urlEntry.value;
        } else {
            var urlList = document.getElementById('urlList');
            if (urlList) {
                url = urlList.value;
            }
        }

        if (url) {
            myPlayer.src([{ src: url, type: 'application/dash+xml' }, ]);
            $('#amve')[0].style.display = 'block';
        }
    });

    $('#urlEntryTxt').click(function () {
        var urlEntry = document.getElementById('urlEntryTxt');
        if (urlEntry && urlEntry.value && urlEntry.value.length > 0 && urlEntry.value === 'Url...') {
            urlEntry.value = '';
        }
    });
}