fromCategory('Lease')
.when({
    LeaseCreated: function(s, e){
        linkTo('LeaseCreated', e);
    }
});
