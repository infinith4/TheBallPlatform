(function(){dust.register("content.dust",body_0);function body_0(chk,ctx){return chk.partial("executeoperation_button.dust",ctx,{"form_name":"fixgroupmastersandcollections","button_label":"Fix Group Masters And Collections","icon_class_name":"icon-plus-sign"}).partial("modal_executeadminoperation_begin.dust",ctx,{"form_name":"fixgroupmastersandcollections","header_title":"Fix Group Masters And Collections","admin_operation_name":"FixGroupMastersAndCollections"}).partial("textinput_singleline.dust",ctx,{"field_id":"FixGroupMastersAndCollections_GroupID","field_name":"GroupID","field_label":"Group ID"}).partial("modal_executeoperation_end.dust",ctx,{"cancel_button_label":"Cancel","accept_button_label":"Fix Group!"});}return body_0;})();