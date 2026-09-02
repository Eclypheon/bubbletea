mergeInto(LibraryManager.library, {
    ResumeWebAudioContext: function () {
        if (typeof WEBAudio !== 'undefined' && WEBAudio.audioContext) {
            if (WEBAudio.audioContext.state === 'suspended') {
                WEBAudio.audioContext.resume();
            }
        }
        var AudioContext = window.AudioContext || window.webkitAudioContext;
        if (AudioContext) {
            var tempCtx = new AudioContext();
            tempCtx.resume().then(function () {
                tempCtx.close();
            });
        }
    }
});
