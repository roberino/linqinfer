using LinqInfer.Data.Serialisation;
using System;

namespace LinqInfer.Maths
{
    public class DataTransformationFactory
    {
        public virtual ISerialisableDataTransformation Create(PortableDataDocument data)
        {
            if (data.IsTypeMatch<SerialisableDataTransformation>())
            {
                return SerialisableDataTransformation.Create(data);
            }

            if (data.IsTypeMatch<Softmax>())
            {
                return Softmax.Create(data);
            }

            if (data.IsTypeMatch<CustomDataTransformation>())
            {
                return CustomDataTransformation.Create(data);
            }

            throw new NotSupportedException(data.TypeName);
        }
    }
}
