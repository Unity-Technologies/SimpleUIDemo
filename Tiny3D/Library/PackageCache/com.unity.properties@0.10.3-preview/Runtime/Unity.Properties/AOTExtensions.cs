namespace Unity.Properties
{
    /// <summary>
    /// No need to ever use this class or call its functions. These only exist to be invoked in generated code paths
    /// (which themselves will never be called at runtime) only to hint to the Ahead Of Time compiler which types
    /// to generate specialized function bodies for.
    /// </summary>
    public static class AOTFunctionGenerator
    {
        // Unused variables
        #pragma warning disable 0219
        public static void GenerateAOTContainerFunctions<TContainer>()
        {
            TContainer container = default(TContainer);
            ChangeTracker changeTracker = default(ChangeTracker);
            
            Actions.GetCollectionCountGetter<TContainer> getCollectionCountGetter = new Actions.GetCollectionCountGetter<TContainer>();
            Actions.GetCountFromActualTypeCallback getCountFromActualTypeCallback = new Actions.GetCountFromActualTypeCallback();
            getCountFromActualTypeCallback.Invoke<TContainer>();
            Actions.GetCountAtPathGetter<TContainer> getCountAtPathGetter = new Actions.GetCountAtPathGetter<TContainer>();
            Actions.VisitAtPathCallback visitAtPathCallback = default;
            visitAtPathCallback.Invoke<TContainer>();
            Actions.SetCountCallback setCountCallback = default;
            setCountCallback.Invoke<TContainer>();
            
            Actions.TryGetCount(ref container, new PropertyPath(), 0, ref changeTracker, out var count);
            Actions.TryGetCountImpl(ref container, new PropertyPath(), 0, ref changeTracker, out var otherCount);
            Actions.GetCount(ref container, new PropertyPath(), 0, ref changeTracker);
        }
        
        public static void GenerateAOTFunctions<TProperty, TContainer, TValue>()
            where TProperty : IProperty<TContainer, TValue>
        {
            TProperty property = default(TProperty);
            TContainer container = default(TContainer);
            TValue value = default(TValue);
            ChangeTracker changeTracker = default(ChangeTracker);
            
            PropertyVisitor propertyVisitor = new PropertyVisitor();
            propertyVisitor.TryVisitContainerWithAdapters(property, ref container, ref value, ref changeTracker);
            propertyVisitor.TryVisitValueWithAdapters(property, ref container, ref value, ref changeTracker);
            PropertyVisitorAdapterExtensions.TryVisitContainer(null, null, property, ref container, ref value, ref changeTracker);
            PropertyVisitorAdapterExtensions.TryVisitValue(null, null, property, ref container, ref value, ref changeTracker);
            
            Actions.GetCollectionCountGetter<TContainer> getCollectionCountGetter = new Actions.GetCollectionCountGetter<TContainer>();
            Actions.GetCountFromActualTypeCallback getCountFromActualTypeCallback = new Actions.GetCountFromActualTypeCallback();
            getCountFromActualTypeCallback.Invoke<TContainer>();
            getCountFromActualTypeCallback.Invoke<TValue>();
            Actions.GetCountAtPathGetter<TContainer> getCountAtPathGetter = new Actions.GetCountAtPathGetter<TContainer>();
            Actions.VisitAtPathCallback visitAtPathCallback = default;
            visitAtPathCallback.Invoke<TContainer>();
            visitAtPathCallback.Invoke<TValue>();
            Actions.SetCountCallback setCountCallback = default;
            setCountCallback.Invoke<TContainer>();
            setCountCallback.Invoke<TValue>();
            
            Actions.TryGetCount(ref container, new PropertyPath(), 0, ref changeTracker, out var count);
            Actions.TryGetCountImpl(ref container, new PropertyPath(), 0, ref changeTracker, out var otherCount);
            Actions.GetCount(ref container, new PropertyPath(), 0, ref changeTracker);
        }
        
        public static void GenerateAOTCollectionFunctions<TProperty, TContainer, TValue>()
            where TProperty : ICollectionProperty<TContainer, TValue>
        {
            TProperty property = default(TProperty);
            TContainer container = default(TContainer);
            TValue value = default(TValue);
            ChangeTracker changeTracker = default(ChangeTracker);
            var getter = new VisitCollectionElementCallback<TContainer>();
            
            PropertyVisitor propertyVisitor = new PropertyVisitor();
            propertyVisitor.TryVisitContainerWithAdapters(property, ref container, ref value, ref changeTracker);
            propertyVisitor.TryVisitCollectionWithAdapters(property, ref container, ref value, ref changeTracker);
            propertyVisitor.TryVisitValueWithAdapters(property, ref container, ref value, ref changeTracker);
            PropertyVisitorAdapterExtensions.TryVisitCollection(null, null, property, ref container, ref value, ref changeTracker);
            PropertyVisitorAdapterExtensions.TryVisitContainer(null, null, property, ref container, ref value, ref changeTracker);
            PropertyVisitorAdapterExtensions.TryVisitValue(null, null, property, ref container, ref value, ref changeTracker);
            
            var arrayProperty = new ArrayProperty<TContainer, TValue>();
            arrayProperty.GetPropertyAtIndex(ref container, 0, ref changeTracker, ref getter);
            var arrayCollectionElementProperty = new ArrayProperty<TContainer, TValue>.CollectionElementProperty();
            arrayCollectionElementProperty.GetValue(ref container);
            arrayCollectionElementProperty.SetValue(ref container, value);
            propertyVisitor.VisitProperty<ArrayProperty<TContainer, TValue>.CollectionElementProperty, TContainer, TValue>(arrayCollectionElementProperty, ref container, ref changeTracker);
            
            var listProperty = new ListProperty<TContainer, TValue>();
            listProperty.GetPropertyAtIndex(ref container, 0, ref changeTracker, ref getter);
            var listCollectionElementProperty = new ListProperty<TContainer, TValue>.CollectionElementProperty();
            listCollectionElementProperty.GetValue(ref container);
            listCollectionElementProperty.SetValue(ref container, value);
            propertyVisitor.VisitProperty<ListProperty<TContainer, TValue>.CollectionElementProperty, TContainer, TValue>(listCollectionElementProperty, ref container, ref changeTracker);
            
            Actions.GetCollectionCountGetter<TContainer> getCollectionCountGetter = new Actions.GetCollectionCountGetter<TContainer>();
            Actions.GetCountFromActualTypeCallback getCountFromActualTypeCallback = new Actions.GetCountFromActualTypeCallback();
            getCountFromActualTypeCallback.Invoke<TContainer>();
            getCountFromActualTypeCallback.Invoke<TValue>();
            Actions.GetCountAtPathGetter<TContainer> getCountAtPathGetter = new Actions.GetCountAtPathGetter<TContainer>();
            Actions.VisitAtPathCallback visitAtPathCallback = default;
            visitAtPathCallback.Invoke<TContainer>();
            visitAtPathCallback.Invoke<TValue>();
            Actions.SetCountCallback setCountCallback = default;
            setCountCallback.Invoke<TContainer>();
            setCountCallback.Invoke<TValue>();
            
            Actions.TryGetCount(ref container, new PropertyPath(), 0, ref changeTracker, out var count);
            Actions.TryGetCountImpl(ref container, new PropertyPath(), 0, ref changeTracker, out var otherCount);
            Actions.GetCount(ref container, new PropertyPath(), 0, ref changeTracker);
        }
        #pragma warning restore 0219 
    }
}