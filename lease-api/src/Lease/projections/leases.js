fromCategory('lease')
.when({
    LeaseCreated: function(s, e){
        emit('leases', 'LeaseCreated', e.body);
    }
});
