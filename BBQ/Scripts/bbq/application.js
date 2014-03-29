﻿var bbq_tmq = {};
(function ($) {
    var url_host;
    var url_c;
    var url_stat;
    var url_c_pxy;
    var url_assemblies;

    setHostAddress("http://127.0.0.1:82/");

    function setHostAddress(address)
    {
        url_host = address;
        url_c = url_host + "tmq/c";
        url_stat = url_host + "tmq/s";
        url_c_pxy = "/bbq/" + "PxySet";
        url_assemblies = url_host + "tmq/assemblies";
    }

    var main_cmodel = null;
    var mods_cmodel = null;
    var assm_cmodel = null;
    var extra_cmodel = null;

    var main_cmodel_id = null;
    var mods_cmodel_id = null;
    var assm_cmodel_id = null;

    var main_synced = false;
    var mods_synced = false;
    var assm_synced = false;
    var extras_synced = false;

    var config_commit_ok = false;

    uuid = (function () {
        // Private array of chars to use
        var CHARS = '0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz'.split('');

        return function (len, radix) {
            var chars = CHARS, uuid = [], rnd = Math.random;
            radix = radix || chars.length;

            if (len) {
                // Compact form
                for (var i = 0; i < len; i++) uuid[i] = chars[0 | rnd() * radix];
            } else {
                // rfc4122, version 4 form
                var r;

                // rfc4122 requires these characters
                uuid[8] = uuid[13] = uuid[18] = uuid[23] = '-';
                uuid[14] = '4';

                // Fill in random data.  At i==19 set the high bits of clock sequence as
                // per rfc4122, sec. 4.1.5
                for (var i = 0; i < 36; i++) {
                    if (!uuid[i]) {
                        r = 0 | rnd() * 16;
                        uuid[i] = chars[(i == 19) ? (r & 0x3) | 0x8 : r & 0xf];
                    }
                }
            }

            return uuid.join('');
        };
    })();

    function resetModels() {
        main_cmodel = bbq_tmq.m_main = {
            "Connections": [
            ],
            "Channels": [
            ],
            "Tasks": [
            ]
        };
        mods_cmodel = bbq_tmq.m_mods = {
            "Modules": [
            ],
        };
        assm_cmodel = bbq_tmq.m_assemblys = {
            "Assemblys": [
            ]
        };
        main_cmodel_id = null;
        mods_cmodel_id = null;
        assm_cmodel_id = null;
    }
    
    function url_cb_js(url) {
        return url + "?format=json&callback=?";
    }

    function getServiceModel(succ, err) {
        main_synced = false; main_cmodel_id = null;
        jsonp(function (data) {// success
            main_synced = true; main_cmodel_id = null;
            main_cmodel = bbq_tmq.m_main = data;
            succ(main_cmodel);
        }, err, { MainPart: true });
    }
    function getModsModel(succ, err) {
        mods_synced = false; mods_cmodel_id = null; 
        jsonp(function (data) {// success
            mods_synced = true; mods_cmodel_id = null;
            mods_cmodel = bbq_tmq.m_mods = data;
            succ(mods_cmodel);
        }, err, { ModulesPart: true });
    }
    function getAssemblysModel(succ, err) {
        assm_synced = false; assm_cmodel_id = null;
        jsonp(function (data) {// success
            assm_synced = true; assm_cmodel_id = null;
            assm_cmodel = bbq_tmq.m_assemblys = data;
            succ(assm_cmodel);
        }, err, { AssemblysPart: true });
    }
    function getExtrasModel(succ, err) {
        extras_synced = false;
        jsonp(function (data) {// success
            extras_synced = true;
            extra_cmodel = bbq_tmq.m_extras = data;
            succ(extra_cmodel);
        }, err, { ConfigurationExtra: true });
    }
    //----------------------------------------------------------------------------===========================
    function jsonp(succ, err, data) {
        $.ajax({ url: url_cb_js(url_c), dataType: "jsonp", data: data, timeout: 10000, cache: false })
            .done(function (data) {
                succ(data);
            }).fail(function () {
                //console.log("error at: " + url_c);
                bbq_tmq.toastr_error(" jsonp unavailable: " + url_c);
                if (err)
                    err();
            });
    }
    function jsonpu(url, succ, err, data) {
        $.ajax({ url: url_cb_js(url), dataType: "jsonp", data: data, timeout: 10000, cache: false })
            .done(function (data) {
                succ(data);
            }).fail(function () {
                //console.log("error at: " + url_c);
                bbq_tmq.toastr_error(" jsonp unavailable: " + url);
                if (err)
                    err();
            });
    }
    function json_proxy(succ, err, data) {
        $.ajax({ url: url_c_pxy, dataType: "json", data: data, timeout: 10000, type: 'POST' })
           .done(function (data) {
               var cresp = angular.fromJson(data);
               succ(cresp);
           }).fail(function () {
               //console.log("error at: " + url_c);
               bbq_tmq.toastr_error(" json proxy unavailable: " + url_c_pxy);
               if (err)
                   err();
           });
    }
    // obj {description, channel, module, parametersStr, intervalType, intervalValue}
    function createTask(obj) {
        var pobj = $.parseJSON(obj.parametersStr);
        var t = {
            Description: obj.description,
            ChannelName: obj.channel,
            ModuleName: obj.module,
            intervalType: obj.intervalType,
            intervalValue: obj.intervalValue,
            parameters: pobj
        };
        main_cmodel.Tasks.push(t);
        mainPartChanged();
    }
    function mainPartChanged() {
        main_cmodel_id = uuid();
    }
    // ==============
    function setServiceModel(succ, err) {
        if (typeof main_cmodel === 'undefined' || main_cmodel === null) {
            succ("mods model not changed since last sync...");
            return;
        }
        json_proxy(function (data) {
            if (data.Result == 'OK') {
                succ(data);
            } else {
                if (err)
                    err();
            }
        }, err, { data: angular.toJson({ MainPart: true, Body: angular.toJson(main_cmodel, false), ConfigId: main_cmodel_id }) });
    }
    function setModsModel(succ, err) {
        //succ({Result:'OK', ConfigCommitID: uuid()});
        if (typeof mods_cmodel_id === 'undefined' || mods_cmodel_id === null) {
            succ("mods model not changed since last sync...");
            return;
        }
        json_proxy(function (data) {
            if (data.Result == 'OK') {
                succ(data);
            } else {
                if (err)
                    err(data.Result);
            }
        }, err, { data: angular.toJson({ ModulesPart: true, Body: angular.toJson(mods_cmodel, false), ConfigId: mods_cmodel_id }) });
    }
    // =========
    function CommitAndReset(succ, err) {
        //succ({ Result: 'OK', ConfigCommitID: uuid() });
        json_proxy(function (data) {
            if (data.Result == 'OK') {
                resetModels();
                succ(data);

            } else {
                if (err)
                    err(data.Result);
            }
        }, err, { data: angular.toJson({ MainPart: main_cmodel_id, ModulesPart: mods_cmodel_id, Reset: true }), urlpostfix: "/commit" });
    }
    //=========
    resetModels();

    function stub() { }
    bbq_tmq = {
        jsonp: jsonpu,
        json_proxy: json_proxy,

        url_stat: url_stat,
        url_assemblies: url_assemblies,

        check_synced: function () { return main_synced && mods_synced && assm_synced && extras_synced; },
        check_commit: function () { return config_commit_ok; },

        m_main: main_cmodel,
        m_mods: mods_cmodel,
        m_assemblys: assm_cmodel,

        rollbackAppC: resetModels, // reset models
        commitandReset: stub,
        commitandRestart: stub,
        syncFrom: getServiceModel,
        syncFromMods: getModsModel,
        syncFromAssemblys: getAssemblysModel,
        syncFromExtras: getExtrasModel,

        createTask: createTask,
        mainPartChanged: mainPartChanged,

        syncToMain: setServiceModel,
        syncToMods: setModsModel,
        CommitAndReset: CommitAndReset,

        setHostAddress: setHostAddress
    };
    //
    jQuery.cachedScript = function (url, options) {
        // allow user to set any option except for dataType, cache, and url
        options = $.extend(options || {}, {
            dataType: "script",
            cache: true,
            url: url
        });
        // Use $.ajax() since it is more flexible than $.getScript
        // Return the jqXHR object so we can chain callbacks
        return jQuery.ajax(options);
    };
    // Extends
    $.cachedScript("Scripts/bbq/application-info.js").done(function (script, textStatus) {
        if (textStatus != "success")
            console.error("js information mod not loaded");
    });
    $.cachedScript("Scripts/bbq/statistic.js").done(function (script, textStatus) {
        if (textStatus != "success")
            console.error("js stats mod not loaded");
    });
    $.cachedScript("Scripts/bbq/assemblies.js").done(function (script, textStatus) {
        if (textStatus != "success")
            console.error("js assemblies mod not loaded");
    });
})(jQuery);

