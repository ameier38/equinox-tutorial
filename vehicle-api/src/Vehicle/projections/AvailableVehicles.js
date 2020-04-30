fromCategory('Vehicle')
.foreachStream()
.when({
    $init: function () {
        return {}
    },
});
