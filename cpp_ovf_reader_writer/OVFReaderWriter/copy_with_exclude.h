#if !defined(COPYWITHEXCLUDE)
#define COPYWITHEXCLUDE

#include <google/protobuf/message.h>
#include <google/protobuf/reflection.h>
#include <list>

using namespace google;
using namespace protobuf;
using namespace std;

namespace open_vector_format{
    void copy_with_exclude(Message* source, Message* target, list<string> excludedFields)
    {
        const Descriptor *target_desc = target->GetDescriptor();

        const Reflection *source_refl = source->GetReflection();  
        const Reflection *target_refl = target->GetReflection();  

        vector<const FieldDescriptor*> source_fields;
        source_refl->ListFields(*source, &source_fields);
        for (long unsigned int i = 0; i < source_fields.size(); i++)
        {
            if (std::find(excludedFields.begin(), excludedFields.end(), source_fields[i]->name()) != excludedFields.end())
            {
                continue;
            }

            if (source_fields[i]->is_repeated() || source_fields[i]->is_map())
            {
                cout << "repeated field name " << source_fields[i]->name() << endl;
                cout << "repeated field name " << source_fields[i]->cpp_type() << endl;
                cout << "length: " << source_refl->FieldSize(*source, source_fields[i]) << endl;
                for (long unsigned int j = 0; j < source_refl->FieldSize(*source, source_fields[i]); j++)
                {
                switch (source_fields[i]->cpp_type())
                {   
                    case FieldDescriptor::CPPTYPE_INT32: 
                        target_refl->AddInt32(target, target_desc->FindFieldByName(source_fields[i]->name()), source_refl->GetRepeatedInt32(*source, source_fields[i], j));
                        break;
                    case FieldDescriptor::CPPTYPE_INT64: 
                        target_refl->AddInt64(target, target_desc->FindFieldByName(source_fields[i]->name()), source_refl->GetRepeatedInt64(*source, source_fields[i], j));
                        break;
                    case FieldDescriptor::CPPTYPE_UINT32: 
                        target_refl->SetUInt32(target, target_desc->FindFieldByName(source_fields[i]->name()), source_refl->GetRepeatedUInt32(*source, source_fields[i], j));
                        break;
                    case FieldDescriptor::CPPTYPE_UINT64: 
                        target_refl->AddUInt64(target, target_desc->FindFieldByName(source_fields[i]->name()), source_refl->GetRepeatedUInt64(*source, source_fields[i], j));
                        break;
                    case FieldDescriptor::CPPTYPE_DOUBLE: 
                        target_refl->AddDouble(target, target_desc->FindFieldByName(source_fields[i]->name()), source_refl->GetRepeatedDouble(*source, source_fields[i], j));
                        break;
                    case FieldDescriptor::CPPTYPE_FLOAT: 
                        target_refl->AddFloat(target, target_desc->FindFieldByName(source_fields[i]->name()), source_refl->GetRepeatedFloat(*source, source_fields[i], j));
                        break;
                    case FieldDescriptor::CPPTYPE_BOOL: 
                        target_refl->AddBool(target, target_desc->FindFieldByName(source_fields[i]->name()), source_refl->GetRepeatedBool(*source, source_fields[i], j));
                        break;
                    case FieldDescriptor::CPPTYPE_ENUM: 
                        target_refl->AddEnum(target, target_desc->FindFieldByName(source_fields[i]->name()), source_refl->GetRepeatedEnum(*source, source_fields[i], j));
                        break;
                    case FieldDescriptor::CPPTYPE_STRING: 
                        target_refl->AddString(target, target_desc->FindFieldByName(source_fields[i]->name()), source_refl->GetRepeatedString(*source, source_fields[i], j));
                        break;
                    case FieldDescriptor::CPPTYPE_MESSAGE:
                    { 
                        auto sub_mes = target_refl->AddMessage(target, target_desc->FindFieldByName(source_fields[i]->name()));
                        sub_mes->CopyFrom(source_refl->GetRepeatedMessage(*source, source_fields[i], j));
                        break;
                    }
                    default:
                        throw runtime_error("Unknown cpp_type " + to_string(source_fields[i]->cpp_type()) + " in field " + source_fields[i]->name());
                }
                }
            }
            else 
            {
            switch (source_fields[i]->cpp_type())
            {
                case FieldDescriptor::CPPTYPE_INT32: 
                    target_refl->SetInt32(target, target_desc->FindFieldByName(source_fields[i]->name()), source_refl->GetInt32(*source, source_fields[i]));
                    break;
                case FieldDescriptor::CPPTYPE_INT64: 
                    target_refl->SetInt64(target, target_desc->FindFieldByName(source_fields[i]->name()), source_refl->GetInt64(*source, source_fields[i]));
                    break;
                case FieldDescriptor::CPPTYPE_UINT32: 
                    target_refl->SetUInt32(target, target_desc->FindFieldByName(source_fields[i]->name()), source_refl->GetUInt32(*source, source_fields[i]));
                    break;
                case FieldDescriptor::CPPTYPE_UINT64: 
                    target_refl->SetUInt64(target, target_desc->FindFieldByName(source_fields[i]->name()), source_refl->GetUInt64(*source, source_fields[i]));
                    break;
                case FieldDescriptor::CPPTYPE_DOUBLE: 
                    target_refl->SetDouble(target, target_desc->FindFieldByName(source_fields[i]->name()), source_refl->GetDouble(*source, source_fields[i]));
                    break;
                case FieldDescriptor::CPPTYPE_FLOAT: 
                    target_refl->SetFloat(target, target_desc->FindFieldByName(source_fields[i]->name()), source_refl->GetFloat(*source, source_fields[i]));
                    break;
                case FieldDescriptor::CPPTYPE_BOOL: 
                    target_refl->SetBool(target, target_desc->FindFieldByName(source_fields[i]->name()), source_refl->GetBool(*source, source_fields[i]));
                    break;
                case FieldDescriptor::CPPTYPE_ENUM: 
                    target_refl->SetEnum(target, target_desc->FindFieldByName(source_fields[i]->name()), source_refl->GetEnum(*source, source_fields[i]));
                    break;
                case FieldDescriptor::CPPTYPE_STRING: 
                    target_refl->SetString(target, target_desc->FindFieldByName(source_fields[i]->name()), source_refl->GetString(*source, source_fields[i]));
                    break;
                case FieldDescriptor::CPPTYPE_MESSAGE:
                { 
                    auto sub_mes = target_refl->MutableMessage(target, target_desc->FindFieldByName(source_fields[i]->name()));
                    sub_mes->CopyFrom(source_refl->GetMessage(*source, source_fields[i]));
                    break;
                }
                default:
                    throw runtime_error("Unknown cpp_type " + to_string(source_fields[i]->cpp_type()) + " in field " + source_fields[i]->name());
            }
            }
        }
    }
}

#endif